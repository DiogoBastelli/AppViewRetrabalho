using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Timers;
using MySql.Data.MySqlClient;
using EquipamentosRetrabalho.Model;

namespace EquipamentosRetrabalho.ViewModel
{
    public class EquipamentosViewModel : INotifyPropertyChanged
    {
        private readonly string _connectionString = "Server=localhost;Database=sew;Uid=root;Pwd=root;";

        // Timer para atualizar a tabela
        private readonly System.Timers.Timer _timer;

        public ObservableCollection<EquipamentosModel> Equipamentos { get; set; } = new();

        private string _filtroPesquisa = "";
        public string FiltroPesquisa
        {
            get => _filtroPesquisa;
            set
            {
                _filtroPesquisa = value;
                OnPropertyChanged();
                CarregarEquipamentos(_filtroPesquisa);
            }
        }

        private string _filtroSelecionado = "cliente";
        public string FiltroSelecionado
        {
            get => _filtroSelecionado;
            set
            {
                _filtroSelecionado = value;
                OnPropertyChanged();
                CarregarEquipamentos(FiltroPesquisa);
            }
        }

        public List<string> FiltrosDisponiveis { get; } = new() { "cliente", "ordem_montagem" };

        public EquipamentosViewModel()
        {
            // Carrega os equipamentos inicialmente
            CarregarEquipamentos();

            // Configura o Timer para atualizar a cada 5 segundos (5000 ms)
            _timer = new System.Timers.Timer(5000);
            _timer.Elapsed += Timer_Elapsed;
            _timer.AutoReset = true;
            _timer.Start();
        }

        private void Timer_Elapsed(object? sender, ElapsedEventArgs e)
        {
            // Atualiza a tabela na thread da UI
            App.Current.Dispatcher.Invoke(() => CarregarEquipamentos(FiltroPesquisa));
        }

        private void CarregarEquipamentos(string filtro = "")
        {
            try
            {
                Equipamentos.Clear();

                using var conn = new MySqlConnection(_connectionString);
                conn.Open();

                string query = "SELECT * FROM controle_lotes";
                if (!string.IsNullOrWhiteSpace(filtro))
                    query += $" WHERE {FiltroSelecionado} LIKE @filtro";

                using var cmd = new MySqlCommand(query, conn);
                if (!string.IsNullOrWhiteSpace(filtro))
                    cmd.Parameters.AddWithValue("@filtro", $"%{filtro}%");

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
