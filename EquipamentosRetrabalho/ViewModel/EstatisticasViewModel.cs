using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using MySql.Data.MySqlClient;

namespace EquipamentosRetrabalho.ViewModel
{
    public class EstatisticasViewModel : INotifyPropertyChanged
    {
        public string QuantidadeRedutoresTexto { get; set; } = "";
        public string QuantidadeMotoresTexto { get; set; } = "";

        public string TotalRetrabalhadosTexto { get; set; } = "";

        public ObservableCollection<string> QuantidadePorTipo { get; set; } = new();
        public ObservableCollection<string> QuantidadePorTipoRedutor { get; set; } = new();

        private readonly string _connectionString = "Server=localhost;Database=sew;Uid=root;Pwd=root;";

        public EstatisticasViewModel()
        {
            CarregarEstatisticas();
        }

        private void CarregarEstatisticas()
        {
            var contagemPorEquipamento = new Dictionary<string, int>();
            var contagemPorTipoRedutor = new Dictionary<string, int>();
            int totalReprovados = 0;
            int totalMotores = 0;
            int totalRedutores = 0;

            var defeitosDeMotor = new List<string> { "fuga", "queimado", "sem giro", "motor", "não gira", "trava" };
            var defeitosDeRedutor = new List<string> { "batida", "engrenagem", "folga", "vazamento", "estalo" };

            using var conn = new MySqlConnection(_connectionString);
            conn.Open();

            string query = "SELECT equipamento, IFNULL(reprovado, 0) AS reprovado, defeito FROM controle_lotes WHERE status = 'Aguardando Retrabalho'";

            using var cmd = new MySqlCommand(query, conn);
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                string equipamento = reader["equipamento"].ToString() ?? "Desconhecido";
                string defeito = reader["defeito"]?.ToString()?.ToLower() ?? "";
                int qtdReprovado = Convert.ToInt32(reader["reprovado"]);

                totalReprovados += qtdReprovado;

                if (contagemPorEquipamento.ContainsKey(equipamento))
                    contagemPorEquipamento[equipamento] += qtdReprovado;
                else
                    contagemPorEquipamento[equipamento] = qtdReprovado;

                if (!string.IsNullOrWhiteSpace(equipamento))
                {
                    string tipo = equipamento.Substring(0, 1).ToUpper();
                    if (contagemPorTipoRedutor.ContainsKey(tipo))
                        contagemPorTipoRedutor[tipo] += qtdReprovado;
                    else
                        contagemPorTipoRedutor[tipo] = qtdReprovado;
                }

                if (defeitosDeMotor.Any(d => defeito.Contains(d)))
                {
                    totalMotores += qtdReprovado;
                }
                else if (defeitosDeRedutor.Any(d => defeito.Contains(d)))
                {
                    totalRedutores += qtdReprovado;
                }
            }

            TotalRetrabalhadosTexto = $"Total de equipamentos em retrabalho: {totalReprovados}";
            QuantidadeMotoresTexto = $"Total de MOTORES com defeito: {totalMotores}";
            QuantidadeRedutoresTexto = $"Total de REDUTORES com defeito: {totalRedutores}";

            QuantidadePorTipo.Clear();
            foreach (var kv in contagemPorEquipamento)
                QuantidadePorTipo.Add($"Equipamento {kv.Key}: {kv.Value} unidades");

            QuantidadePorTipoRedutor.Clear();
            foreach (var kv in contagemPorTipoRedutor)
                QuantidadePorTipoRedutor.Add($"Tipo {kv.Key}: {kv.Value} unidades");

            OnPropertyChanged(nameof(TotalRetrabalhadosTexto));
            OnPropertyChanged(nameof(QuantidadeMotoresTexto));
            OnPropertyChanged(nameof(QuantidadeRedutoresTexto));
            OnPropertyChanged(nameof(QuantidadePorTipo));
            OnPropertyChanged(nameof(QuantidadePorTipoRedutor));
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? nome = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nome));
        }
    }
}
