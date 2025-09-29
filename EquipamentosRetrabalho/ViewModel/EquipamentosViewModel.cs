using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Timers;
using MySql.Data.MySqlClient;
using EquipamentosRetrabalho.Model;
using System.Linq;
using System.Collections.Generic;
using System.Windows;

namespace EquipamentosRetrabalho.ViewModel
{
    public class EquipamentosViewModel : INotifyPropertyChanged
    {
        private readonly string _connectionString = "Server=localhost;Database=sew;Uid=root;Pwd=root;";

        private readonly System.Timers.Timer _timer;

        public ObservableCollection<OpcaoFiltro> OpcoesFiltro { get; } = new()
        {
            new OpcaoFiltro { Nome = "Família R" },
            new OpcaoFiltro { Nome = "Família K" },
            new OpcaoFiltro { Nome = "Família S" },
            new OpcaoFiltro { Nome = "Família F" },
            new OpcaoFiltro { Nome = "Do Mais Novo pro Mais Antigo" },
            new OpcaoFiltro { Nome = "Do Mais Antigo pro Mais Novo" }
        };

        public ObservableCollection<EquipamentosModel> Equipamentos { get; set; } = new();

        private string _textoPesquisa = string.Empty;
        public string TextoPesquisa
        {
            get => _textoPesquisa;
            set
            {
                _textoPesquisa = value;
                OnPropertyChanged();
                CarregarEquipamentosFiltro(_textoPesquisa);
            }
        }

        private string _campoPesquisaSelecionado = "cliente";
        public string CampoPesquisaSelecionado
        {
            get => _campoPesquisaSelecionado;
            set
            {
                _campoPesquisaSelecionado = value;
                OnPropertyChanged();
                CarregarEquipamentosFiltro(TextoPesquisa);
            }
        }

        public List<string> CamposDisponiveis { get; } = new() { "cliente", "ordem_montagem" };

        // Construtor
        public EquipamentosViewModel()
        {
            CarregarEquipamentosFiltro();

            foreach (var opcao in OpcoesFiltro)
            {
                opcao.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(OpcaoFiltro.Selecionado))
                    {
                        // Garante que só um filtro de ordenação fique ativo
                        if (opcao.Selecionado &&
                           (opcao.Nome == "Do Mais Novo pro Mais Antigo" || opcao.Nome == "Do Mais Antigo pro Mais Novo"))
                        {
                            foreach (var other in OpcoesFiltro)
                            {
                                if (other != opcao &&
                                   (other.Nome == "Do Mais Novo pro Mais Antigo" || other.Nome == "Do Mais Antigo pro Mais Novo"))
                                {
                                    other.Selecionado = false;
                                }
                            }
                        }

                        CarregarEquipamentosFiltro(TextoPesquisa);
                    }
                };
            }

            _timer = new System.Timers.Timer(5000);
            _timer.Elapsed += Timer_Elapsed;
            _timer.AutoReset = true;
            _timer.Start();
        }

        private void Timer_Elapsed(object? sender, ElapsedEventArgs e)
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                CarregarEquipamentosFiltro(TextoPesquisa);
            });
        }

        private void CarregarEquipamentosFiltro(string pesquisa = "")
        {
            try
            {
                Equipamentos.Clear();

                var familiasSelecionadas = OpcoesFiltro
                    .Where(f => f.Selecionado && f.Nome.StartsWith("Família"))
                    .Select(f => f.Nome.Split(' ').Last())
                    .ToList();

                bool ordenarNovoAntigo = OpcoesFiltro.Any(f => f.Selecionado && f.Nome == "Do Mais Novo pro Mais Antigo");
                bool ordenarAntigoNovo = OpcoesFiltro.Any(f => f.Selecionado && f.Nome == "Do Mais Antigo pro Mais Novo");

                using var conn = new MySqlConnection(_connectionString);
                conn.Open();

                string query = "SELECT * FROM controle_lotes";
                List<string> conditions = new();

                // Filtro por pesquisa
                if (!string.IsNullOrWhiteSpace(pesquisa))
                    conditions.Add($"{CampoPesquisaSelecionado} LIKE @pesquisa");

                // Filtro por família
                if (familiasSelecionadas.Any())
                {
                    var conds = familiasSelecionadas
                        .Select((letra, index) => $"equipamento LIKE @fam{index}");
                    conditions.Add("(" + string.Join(" OR ", conds) + ")");
                }

                // Junta as condições
                if (conditions.Any())
                    query += " WHERE " + string.Join(" AND ", conditions);

                // Ordenação
                if (ordenarNovoAntigo)
                    query += " ORDER BY data DESC";
                else if (ordenarAntigoNovo)
                    query += " ORDER BY data ASC";

                using var cmd = new MySqlCommand(query, conn);

                if (!string.IsNullOrWhiteSpace(pesquisa))
                    cmd.Parameters.AddWithValue("@pesquisa", $"%{pesquisa}%");

                for (int i = 0; i < familiasSelecionadas.Count; i++)
                    cmd.Parameters.AddWithValue($"@fam{i}", familiasSelecionadas[i] + "%");

                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    Equipamentos.Add(new EquipamentosModel
                    {
                        OrdemMontagem = reader["ordem_montagem"]?.ToString() ?? string.Empty,
                        OrdemVenda = reader["ordem_venda"]?.ToString() ?? string.Empty,
                        Cliente = reader["cliente"]?.ToString() ?? string.Empty,
                        ItemVenda = reader["item_venda"]?.ToString() ?? string.Empty,
                        EquipamentoNome = reader["equipamento"]?.ToString() ?? string.Empty,
                        QuantidadeTotal = reader.IsDBNull(reader.GetOrdinal("quantidade_total")) ? null : reader.GetInt32("quantidade_total"),
                        Reprovado = reader.IsDBNull(reader.GetOrdinal("reprovado")) ? null : reader.GetInt32("reprovado"),
                        Data = reader.IsDBNull(reader.GetOrdinal("data")) ? null : reader.GetDateTime("data"),
                        Defeito = reader["defeito"]?.ToString() ?? string.Empty,
                        Status = reader["status"]?.ToString() ?? string.Empty,
                        Local = reader["local"]?.ToString() ?? string.Empty,
                        DataFinalizacao = reader.IsDBNull(reader.GetOrdinal("data_finalizacao")) ? null : reader.GetDateTime("data_finalizacao")
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Erro ao carregar equipamentos: " + ex.Message);
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? nome = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nome));
        }
    }
}
