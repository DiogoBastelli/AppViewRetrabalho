using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using MySql.Data.MySqlClient;
using EquipamentosRetrabalho.Model;

namespace EquipamentosRetrabalho.ViewModel
{
    public class EquipamentosViewModel : INotifyPropertyChanged
    {
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

        private readonly string _connectionString = "Server=localhost;Database=sew;Uid=root;Pwd=root;";

        public EquipamentosViewModel()
        {
            CarregarEquipamentos();
        }

        private void CarregarEquipamentos(string filtro = "")
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
                    OrdemMontagem = reader["ordem_montagem"].ToString(),
                    OrdemVenda = reader["ordem_venda"].ToString(),
                    Cliente = reader["cliente"].ToString(),
                    ItemVenda = reader["item_venda"].ToString(),
                    EquipamentoNome = reader["equipamento"].ToString(),
                    QuantidadeTotal = reader.GetInt32("quantidade_total"),
                    Reprovado = reader.IsDBNull(reader.GetOrdinal("reprovado")) ? null : reader.GetInt32("reprovado"),
                    Data = reader.IsDBNull(reader.GetOrdinal("data")) ? null : reader.GetDateTime("data"),
                    Defeito = reader["defeito"]?.ToString(),
                    Status = reader["status"]?.ToString(),
                    Local = reader["local"]?.ToString()
                });
            }
        }


        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string nome = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nome));
        }

        public List<string> FiltrosDisponiveis { get; } = new() { "cliente", "ordem_montagem" };

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

    }
}
