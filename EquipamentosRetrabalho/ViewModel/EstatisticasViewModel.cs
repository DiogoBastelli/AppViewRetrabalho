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
        public IEnumerable<ISeries> RedutoresPieSeries { get; set; } = Array.Empty<ISeries>();

        public LiveChartsCore.Drawing.Padding DrawMargin { get; set; } = new(0, 0, 0, 0);

        public string QuantidadeRedutoresTexto { get; set; } = "";
        public string QuantidadeMotoresTexto { get; set; } = "";
        public string TotalRetrabalhadosTexto { get; set; } = "";

        public ObservableCollection<string> QuantidadePorTipo { get; set; } = new();
        public ObservableCollection<string> QuantidadePorTipoRedutor { get; set; } = new();

        // Propriedades para o gráfico
        public ISeries[] Series { get; set; } = System.Array.Empty<ISeries>();
        public Axis[] XAxes { get; set; } = System.Array.Empty<Axis>();
        public Axis[] YAxes { get; set; } = System.Array.Empty<Axis>();

        private readonly string _connectionString = "Server=localhost;Database=sew;Uid=root;Pwd=root;";

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
            var contagemPorEquipamento = new Dictionary<string, int>();
            var contagemPorTipoRedutor = new Dictionary<string, int>();

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

                    if (motoresPorMes.ContainsKey(mes))
                        motoresPorMes[mes] += qtdReprovado;
                    else
                        motoresPorMes[mes] = qtdReprovado;
                }
                else if (defeitosDeRedutor.Any(d => defeito.Contains(d)))
                {
                    totalRedutores += qtdReprovado;

                    if (redutoresPorMes.ContainsKey(mes))
                        redutoresPorMes[mes] += qtdReprovado;
                    else
                        redutoresPorMes[mes] = qtdReprovado;
                }
            }

            TotalRetrabalhadosTexto = $"Total de equipamentos em retrabalho: {totalReprovados}";
            QuantidadeMotoresTexto = $"Total de MOTORES com defeito: {totalMotores}";
            QuantidadeRedutoresTexto = $"Total de REDUTORES com defeito: {totalRedutores}";

            QuantidadePorTipo.Clear();
            foreach (var kv in contagemPorEquipamento.OrderBy(k => k.Key))
                QuantidadePorTipo.Add($"Equipamento {kv.Key}: {kv.Value} unidades");

            QuantidadePorTipoRedutor.Clear();
            foreach (var kv in contagemPorTipoRedutor.OrderBy(k => k.Key))
                QuantidadePorTipoRedutor.Add($"Tipo {kv.Key}: {kv.Value} unidades");

            var mesesComDados = motoresPorMes.Keys.Union(redutoresPorMes.Keys).Distinct().OrderBy(m => m).ToList();
            string[] nomeDosMeses = System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.MonthNames;
            var labels = mesesComDados.Select(m => nomeDosMeses[m - 1]).ToArray();

            var motoresValores = mesesComDados.Select(m => motoresPorMes.ContainsKey(m) ? motoresPorMes[m] : 0).ToArray();
            var redutoresValores = mesesComDados.Select(m => redutoresPorMes.ContainsKey(m) ? redutoresPorMes[m] : 0).ToArray();

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

            XAxes = new Axis[]
            {
                new Axis
                {
                    Labels = labels,
                    SeparatorsPaint = new SolidColorPaint(SKColors.LightGray)
                }
            };

            DrawMargin = new LiveChartsCore.Drawing.Padding(0, 0, 0, 0); // margem do gráfico

            YAxes = new Axis[]
            {
            new Axis
            {
                Labeler = value => ((int)value).ToString(),
                MinLimit = 0,
                Padding = new LiveChartsCore.Drawing.Padding(0, 0, 10, 0), // controla espaço à direita do eixo
                SeparatorsPaint = new SolidColorPaint(SKColors.LightGray)
            }
                    };



            OnPropertyChanged(nameof(TotalRetrabalhadosTexto));
            OnPropertyChanged(nameof(QuantidadeMotoresTexto));
            OnPropertyChanged(nameof(QuantidadeRedutoresTexto));
            OnPropertyChanged(nameof(QuantidadePorTipo));
            OnPropertyChanged(nameof(QuantidadePorTipoRedutor));
            OnPropertyChanged(nameof(Series));
            OnPropertyChanged(nameof(XAxes));
            OnPropertyChanged(nameof(YAxes));

            RedutoresPieSeries = contagemPorTipoRedutor.Select(kv =>
                new PieSeries<int>
                {
                    Name = kv.Key,
                    Values = new[] { kv.Value },
                    DataLabelsPaint = new SolidColorPaint(SKColors.White),
                    DataLabelsPosition = PolarLabelsPosition.Middle,
                    DataLabelsFormatter = point => $"{point.Context.Series.Name}: {point.PrimaryValue}"
                }).ToArray();

            OnPropertyChanged(nameof(RedutoresPieSeries));


        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? nome = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nome));
    }
}
