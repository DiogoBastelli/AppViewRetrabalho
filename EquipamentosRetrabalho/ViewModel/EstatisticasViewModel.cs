using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using MySql.Data.MySqlClient;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.Measure;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;

namespace EquipamentosRetrabalho.ViewModel
{
    public class EstatisticasViewModel : INotifyPropertyChanged
    {
        // Strings para exibir nas estatísticas gerais
        public string QuantidadeRedutoresTexto { get; set; } = "";
        public string QuantidadeMotoresTexto { get; set; } = "";
        public string TotalRetrabalhadosTexto { get; set; } = "";

        // Listas para exibir quantidades por equipamento e tipo
        public ObservableCollection<string> QuantidadePorTipo { get; set; } = new();
        public ObservableCollection<string> QuantidadePorTipoRedutor { get; set; } = new();

        // Propriedades para o gráfico
        public ISeries[] Series { get; set; } = System.Array.Empty<ISeries>();
        public Axis[] XAxes { get; set; } = System.Array.Empty<Axis>();
        public Axis[] YAxes { get; set; } = System.Array.Empty<Axis>();

        private readonly string _connectionString = "Server=localhost;Database=sew;Uid=root;Pwd=root;";

        // Termos para classificar defeitos como motor ou redutor (ajuste conforme seu cenário)
        private readonly List<string> defeitosDeMotor = new()
        {
            "fuga","curto"
        };

        private readonly List<string> defeitosDeRedutor = new()
        {
            "batida",
        };

        public EstatisticasViewModel()
        {
            CarregarEstatisticas();
        }

        private void CarregarEstatisticas()
        {
            // Dicionários para contagem geral
            var contagemPorEquipamento = new Dictionary<string, int>();
            var contagemPorTipoRedutor = new Dictionary<string, int>();

            // Dicionários para contagem por mês (gráfico)
            var motoresPorMes = new Dictionary<int, int>();
            var redutoresPorMes = new Dictionary<int, int>();

            int totalReprovados = 0;
            int totalMotores = 0;
            int totalRedutores = 0;

            using var conn = new MySqlConnection(_connectionString);
            conn.Open();

            string query = "SELECT equipamento, IFNULL(reprovado, 0) AS reprovado, defeito, MONTH(data) AS mes FROM controle_lotes WHERE status = 'Aguardando Retrabalho'";

            using var cmd = new MySqlCommand(query, conn);
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                string equipamento = reader["equipamento"]?.ToString() ?? "Desconhecido";
                string defeito = reader["defeito"]?.ToString()?.ToLower() ?? "";
                int qtdReprovado = Convert.ToInt32(reader["reprovado"]);
                int mes = Convert.ToInt32(reader["mes"]);

                totalReprovados += qtdReprovado;

                // Estatísticas gerais por equipamento
                if (contagemPorEquipamento.ContainsKey(equipamento))
                    contagemPorEquipamento[equipamento] += qtdReprovado;
                else
                    contagemPorEquipamento[equipamento] = qtdReprovado;

                // Estatísticas gerais por tipo (primeira letra do equipamento)
                if (!string.IsNullOrWhiteSpace(equipamento))
                {
                    string tipo = equipamento.Substring(0, 1).ToUpper();
                    if (contagemPorTipoRedutor.ContainsKey(tipo))
                        contagemPorTipoRedutor[tipo] += qtdReprovado;
                    else
                        contagemPorTipoRedutor[tipo] = qtdReprovado;
                }

                // Contagem motores/redutores gerais
                if (defeitosDeMotor.Any(d => defeito.Contains(d)))
                {
                    totalMotores += qtdReprovado;

                    // Contagem motores por mês para gráfico
                    if (motoresPorMes.ContainsKey(mes))
                        motoresPorMes[mes] += qtdReprovado;
                    else
                        motoresPorMes[mes] = qtdReprovado;
                }
                else if (defeitosDeRedutor.Any(d => defeito.Contains(d)))
                {
                    totalRedutores += qtdReprovado;

                    // Contagem redutores por mês para gráfico
                    if (redutoresPorMes.ContainsKey(mes))
                        redutoresPorMes[mes] += qtdReprovado;
                    else
                        redutoresPorMes[mes] = qtdReprovado;
                }
            }

            // Atualiza textos gerais
            TotalRetrabalhadosTexto = $"Total de equipamentos em retrabalho: {totalReprovados}";
            QuantidadeMotoresTexto = $"Total de MOTORES com defeito: {totalMotores}";
            QuantidadeRedutoresTexto = $"Total de REDUTORES com defeito: {totalRedutores}";

            // Atualiza listas para exibição na UI
            QuantidadePorTipo.Clear();
            foreach (var kv in contagemPorEquipamento.OrderBy(k => k.Key))
                QuantidadePorTipo.Add($"Equipamento {kv.Key}: {kv.Value} unidades");

            QuantidadePorTipoRedutor.Clear();
            foreach (var kv in contagemPorTipoRedutor.OrderBy(k => k.Key))
                QuantidadePorTipoRedutor.Add($"Tipo {kv.Key}: {kv.Value} unidades");

            // Preparar labels dos meses para o gráfico
            var mesesComDados = motoresPorMes.Keys.Union(redutoresPorMes.Keys).Distinct().OrderBy(m => m).ToList();
            string[] nomeDosMeses = System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.MonthNames;
            var labels = mesesComDados.Select(m => nomeDosMeses[m - 1]).ToArray();

            // Valores por mês para gráfico, colocando zero se não houver dados naquele mês
            var motoresValores = mesesComDados.Select(m => motoresPorMes.ContainsKey(m) ? motoresPorMes[m] : 0).ToArray();
            var redutoresValores = mesesComDados.Select(m => redutoresPorMes.ContainsKey(m) ? redutoresPorMes[m] : 0).ToArray();

            // Define as séries do gráfico
            Series = new ISeries[]
            {
                new LiveChartsCore.SkiaSharpView.ColumnSeries<int>
                {
                    Values = motoresValores,
                    Name = "Motores"
                },
                new LiveChartsCore.SkiaSharpView.ColumnSeries<int>
                {
                    Values = redutoresValores,
                    Name = "Redutores"
                }
            };

            // Configura eixo X com os meses
            XAxes = new Axis[]
            {
                new Axis
                {
                    Labels = labels,
                    SeparatorsPaint = new SolidColorPaint(SKColors.LightGray)
                }
            };

            // Configura eixo Y para valores inteiros
            YAxes = new Axis[]
            {
                new Axis
                {
                    Labeler = value => ((int)value).ToString(),
                    SeparatorsPaint = new SolidColorPaint(SKColors.LightGray)
                }
            };

            // Dispara notificações para UI atualizar bindings
            OnPropertyChanged(nameof(TotalRetrabalhadosTexto));
            OnPropertyChanged(nameof(QuantidadeMotoresTexto));
            OnPropertyChanged(nameof(QuantidadeRedutoresTexto));
            OnPropertyChanged(nameof(QuantidadePorTipo));
            OnPropertyChanged(nameof(QuantidadePorTipoRedutor));
            OnPropertyChanged(nameof(Series));
            OnPropertyChanged(nameof(XAxes));
            OnPropertyChanged(nameof(YAxes));
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? nome = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nome));
    }
}
