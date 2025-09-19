using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Timers;
using MySql.Data.MySqlClient;
using EquipamentosRetrabalho.Model;
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
            new OpcaoFiltro { Nome = "Família F" }
        };
        
        public ObservableCollection<EquipamentosModel> Equipamentos { get; set; } = new();

        private string _textoPesquisa = "";
        public string TextoPesquisa
        {
            get => _textoPesquisa;
            set
            {
                _textoPesquisa = value;
                OnPropertyChanged();
                CarregarEquipamentos(_textoPesquisa);
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
                CarregarEquipamentos(TextoPesquisa);
            }
        }

        public List<string> CamposDisponiveis { get; } = new() { "cliente", "ordem_montagem" };

        public EquipamentosViewModel()
        {
            CarregarEquipamentos();

            foreach (var opcao in OpcoesFiltro)
            {
                opcao.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(OpcaoFiltro.Selecionado))
                    {
                        CarregarEquipamentosFiltro(); 
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
                if (OpcoesFiltro.Any(f => f.Selecionado))
                    CarregarEquipamentosFiltro();
                else
                    CarregarEquipamentos(TextoPesquisa);
            });
        }


        private void CarregarEquipamentos(string pesquisa = "")
        {
            try
            {
                Equipamentos.Clear();

                using var conn = new MySqlConnection(_connectionString);
                conn.Open();

                string query = "SELECT * FROM controle_lotes";
                if (!string.IsNullOrWhiteSpace(pesquisa))
                    query += $" WHERE {CampoPesquisaSelecionado} LIKE @pesquisa";

                using var cmd = new MySqlCommand(query, conn);
                if (!string.IsNullOrWhiteSpace(pesquisa))
                    cmd.Parameters.AddWithValue("@pesquisa", $"%{pesquisa}%");

                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    Equipamentos.Add(new EquipamentosModel
                    {
                        OrdemMontagem = reader["ordem_montagem"]?.ToString(),
                        OrdemVenda = reader["ordem_venda"]?.ToString(),
                        Cliente = reader["cliente"]?.ToString(),
                        ItemVenda = reader["item_venda"]?.ToString(),
                        EquipamentoNome = reader["equipamento"]?.ToString(),
                        QuantidadeTotal = reader.IsDBNull(reader.GetOrdinal("quantidade_total")) ? null : reader.GetInt32("quantidade_total"),
                        Reprovado = reader.IsDBNull(reader.GetOrdinal("reprovado")) ? null : reader.GetInt32("reprovado"),
                        Data = reader.IsDBNull(reader.GetOrdinal("data")) ? null : reader.GetDateTime("data"),
                        Defeito = reader["defeito"]?.ToString(),
                        Status = reader["status"]?.ToString(),
                        Local = reader["local"]?.ToString(),
                        DataFinalizacao = reader.IsDBNull(reader.GetOrdinal("data_finalizacao")) ? null : reader.GetDateTime("data_finalizacao")
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Erro ao carregar equipamentos: " + ex.Message);
            }
        }

        private void CarregarEquipamentosFiltro()
        {
            try
            {
                Equipamentos.Clear();

                var filtrosSelecionados = OpcoesFiltro
                    .Where(f => f.Selecionado)
                    .Select(f => f.Nome.Split(' ').Last()) 
                    .ToList();

                using var conn = new MySqlConnection(_connectionString);
                conn.Open();

                string query = "SELECT * FROM controle_lotes";

                if (filtrosSelecionados.Any())
                {
                    var conditions = filtrosSelecionados
                        .Select((letra, index) => $"equipamento LIKE @letra{index}");
                    query += " WHERE " + string.Join(" OR ", conditions);
                }

                using var cmd = new MySqlCommand(query, conn);

                for (int i = 0; i < filtrosSelecionados.Count; i++)
                {
                    cmd.Parameters.AddWithValue($"@letra{i}", filtrosSelecionados[i] + "%");
                }

                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    Equipamentos.Add(new EquipamentosModel
                    {
                        OrdemMontagem = reader["ordem_montagem"]?.ToString(),
                        OrdemVenda = reader["ordem_venda"]?.ToString(),
                        Cliente = reader["cliente"]?.ToString(),
                        ItemVenda = reader["item_venda"]?.ToString(),
                        EquipamentoNome = reader["equipamento"]?.ToString(),
                        QuantidadeTotal = reader.IsDBNull(reader.GetOrdinal("quantidade_total")) ? null : reader.GetInt32("quantidade_total"),
                        Reprovado = reader.IsDBNull(reader.GetOrdinal("reprovado")) ? null : reader.GetInt32("reprovado"),
                        Data = reader.IsDBNull(reader.GetOrdinal("data")) ? null : reader.GetDateTime("data"),
                        Defeito = reader["defeito"]?.ToString(),
                        Status = reader["status"]?.ToString(),
                        Local = reader["local"]?.ToString(),
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
