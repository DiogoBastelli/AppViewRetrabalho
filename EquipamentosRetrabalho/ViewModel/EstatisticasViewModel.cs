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
using System.Text;
using System.Windows;
using LiveChartsCore.Themes;
using System.Windows.Controls.Primitives;
using System.Windows.Controls;

namespace EquipamentosRetrabalho.ViewModel
{
    public class EstatisticasViewModel : INotifyPropertyChanged
    {
        public IEnumerable<ISeries> RedutoresPieSeries { get; set; } = Array.Empty<ISeries>();

        public IEnumerable<ISeries> TempoMedioFinalização { get; set; } = Array.Empty<ISeries>();

        public LiveChartsCore.Drawing.Padding DrawMargin { get; set; } = new(0, 0, 0, 0);

        public string QuantidadeRedutoresTexto { get; set; } = "";
        public string QuantidadeMotoresTexto { get; set; } = "";
        public string TotalRetrabalhadosTexto { get; set; } = "";

        private string _quantidadePorDefeitoTexto = "";
        public string QuantidadePorDefeitoTexto
        {
            get => _quantidadePorDefeitoTexto;
            set
            {
                _quantidadePorDefeitoTexto = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<string> QuantidadePorTipo { get; set; } = new();
        public ObservableCollection<string> QuantidadePorTipoRedutor { get; set; } = new();
        public ObservableCollection<DefeitoEstatistica> DefeitosOrdenados { get; set; } = new();


        public ISeries[] Series { get; set; } = Array.Empty<ISeries>();
        public Axis[] XAxes { get; set; } = Array.Empty<Axis>();
        public Axis[] YAxes { get; set; } = Array.Empty<Axis>();


        private readonly string _connectionString = "Server=localhost;Database=sew;Uid=root;Pwd=root;";


        private string _mediaTempo;
        public string MediaTempo
        {
            get => _mediaTempo;
            set
            {
                _mediaTempo = value;
                OnPropertyChanged();
            }
        }


        public class DefeitoEstatistica
        {
            public int Posicao { get; set; }      
            public string Nome { get; set; } = "";
            public double Percentual { get; set; }
            public string PercentualFormatado => $"{Percentual}%";
            public string PosicaoFormatada => $"{Posicao}º";
        }

        private readonly List<string> defeitosDeMotor = new()
        {
            "fuga","curto", "corrente alta" , "freio" , "resina" , "ruido motor"
        };

        private readonly List<string> defeitosDeRedutor = new()
        {
            "batida","batida pinhao" , "batida entrada" , "batida intermediaria" , "batida saida" , "ruido redutor",
        };

        public EstatisticasViewModel()
        {
            CarregarEstatisticas();
            //CalcularMediaTempo();
            CalcularMediaPorTipo();
        }

        private void CarregarEstatisticas()
        {
            var contagemPorEquipamento = new Dictionary<string, int>();
            var contagemPorTipoRedutor = new Dictionary<string, int>();
            var contagemPorDefeito = new Dictionary<string, int>();

            var motoresPorMes = new Dictionary<int, int>();
            var redutoresPorMes = new Dictionary<int, int>();

            int totalReprovados = 0;
            int totalMotores = 0;
            int totalRedutores = 0;

            using var conn = new MySqlConnection(_connectionString);
            conn.Open();

            string queryPrincipal = @"
            SELECT equipamento, IFNULL(reprovado, 0) AS reprovado, defeito, MONTH(data) AS mes
            FROM controle_lotes
            WHERE status = 'Aguardando Retrabalho'";

            using (var cmd = new MySqlCommand(queryPrincipal, conn))
            using (var reader = cmd.ExecuteReader())
            {
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

                    if (contagemPorDefeito.ContainsKey(defeito))
                        contagemPorDefeito[defeito] += qtdReprovado;
                    else
                        contagemPorDefeito[defeito] = qtdReprovado;

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
            }

            TotalRetrabalhadosTexto = $"Total: {totalReprovados}";
            QuantidadeMotoresTexto = $"Motor: {totalMotores}";
            QuantidadeRedutoresTexto = $"Redutor: {totalRedutores}";

            DefeitosOrdenados.Clear();

            int pos = 1;
            foreach (var kv in contagemPorDefeito.OrderByDescending(kv => kv.Value))
            {
                double porcentagem = totalReprovados > 0 ? (kv.Value * 100.0) / totalReprovados : 0;

                DefeitosOrdenados.Add(new DefeitoEstatistica
                {
                    Posicao = pos++,
                    Nome = kv.Key,
                    Percentual = Math.Round(porcentagem, 1)
                });
            }

            OnPropertyChanged(nameof(DefeitosOrdenados));


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
            new LiveChartsCore.SkiaSharpView.ColumnSeries<int> { Values = motoresValores, Name = "Motores" },
            new LiveChartsCore.SkiaSharpView.ColumnSeries<int> { Values = redutoresValores, Name = "Redutores" }
            };

            XAxes = new Axis[] { new Axis { Labels = labels, SeparatorsPaint = new SolidColorPaint(SKColors.LightGray) } };
            YAxes = new Axis[] { new Axis { Labeler = value => ((int)value).ToString(), MinLimit = 0, Padding = new LiveChartsCore.Drawing.Padding(0, 0, 10, 0), SeparatorsPaint = new SolidColorPaint(SKColors.LightGray) } };

            RedutoresPieSeries = contagemPorTipoRedutor.Select(kv =>
                new PieSeries<int>
                {
                    Name = kv.Key,
                    Values = new[] { kv.Value },
                    DataLabelsPaint = new SolidColorPaint(SKColors.White),
                    DataLabelsPosition = PolarLabelsPosition.Middle,
                    DataLabelsFormatter = point => $"{point.Context.Series.Name}: {point.PrimaryValue}"
                }).ToArray();

            OnPropertyChanged(nameof(TotalRetrabalhadosTexto));
            OnPropertyChanged(nameof(QuantidadeMotoresTexto));
            OnPropertyChanged(nameof(QuantidadeRedutoresTexto));
            OnPropertyChanged(nameof(QuantidadePorDefeitoTexto));
            OnPropertyChanged(nameof(QuantidadePorTipoRedutor));
            OnPropertyChanged(nameof(Series));
            OnPropertyChanged(nameof(XAxes));
            OnPropertyChanged(nameof(YAxes));
            OnPropertyChanged(nameof(RedutoresPieSeries));
        }

        public void CalcularMediaTempo()
        {
            try
            {
                using var conn = new MySqlConnection(_connectionString);
                conn.Open();

                string query = @"
                SELECT TIMESTAMPDIFF(SECOND, data, data_finalizacao) AS segundos
                FROM controle_lotes
                WHERE data_finalizacao IS NOT NULL";

                using var cmd = new MySqlCommand(query, conn);
                using var reader = cmd.ExecuteReader();

                var listaSegundos = new List<int>();

                while (reader.Read())
                {
                    if (!reader.IsDBNull(reader.GetOrdinal("segundos")))
                    {
                        listaSegundos.Add(reader.GetInt32("segundos"));
                    }
                }

                if (listaSegundos.Any())
                {
                    var mediaSegundos = listaSegundos.Average();
                    var media = TimeSpan.FromSeconds(mediaSegundos);

                    MediaTempo = $"Média: {media.Days} dias, {media.Hours} horas e {media.Minutes} minutos";
                }
                else
                {
                    MediaTempo = "Nenhuma ordem finalizada ainda.";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao calcular média: {ex.Message}");
            }
        }

        private string _mediaPorTipoTexto;
        public string MediaPorTipoTexto
        {
            get => _mediaPorTipoTexto;
            set
            {
                _mediaPorTipoTexto = value;
                OnPropertyChanged();
            }
        }

        private string? _mesSelecionadoTipoRedutor;
        public string? MesSelecionadoTipoRedutor
        {
            get => _mesSelecionadoTipoRedutor;
            set
            {
                _mesSelecionadoTipoRedutor = value;
                OnPropertyChanged();
                int mes = ObterNumeroMes(value ?? "Todos");
                CalcularRedutoresPorTipo(mes);
            }
        }


        public void CalcularRedutoresPorTipo(int mes = 0)
        {
            using var conn = new MySqlConnection(_connectionString);
            conn.Open();

            string query = @"
        SELECT equipamento, IFNULL(reprovado, 0) AS reprovado
        FROM controle_lotes
        WHERE status='Aguardando Retrabalho'";

            if (mes > 0)
                query += " AND MONTH(data) = @mes";

            using var cmd = new MySqlCommand(query, conn);
            if (mes > 0)
                cmd.Parameters.AddWithValue("@mes", mes);

            using var reader = cmd.ExecuteReader();

            var contagemPorTipoRedutor = new Dictionary<string, int>();
            int totalMotores = 0;
            int totalRedutores = 0;
            int totalReprovados = 0;

            while (reader.Read())
            {
                string equipamento = reader["equipamento"].ToString();
                int qtdReprovado = Convert.ToInt32(reader["reprovado"]);
                string tipo = equipamento.Substring(0, 1).ToUpper();

                totalReprovados += qtdReprovado;

                if (defeitosDeMotor.Any(d => equipamento.ToLower().Contains(d)))
                    totalMotores += qtdReprovado;
                else if (defeitosDeRedutor.Any(d => equipamento.ToLower().Contains(d)))
                    totalRedutores += qtdReprovado;

                if (contagemPorTipoRedutor.ContainsKey(tipo))
                    contagemPorTipoRedutor[tipo] += qtdReprovado;
                else
                    contagemPorTipoRedutor[tipo] = qtdReprovado;
            }

            // Atualiza os textos
            TotalRetrabalhadosTexto = $"Total: {totalReprovados}";
            QuantidadeMotoresTexto = $"Motor: {totalMotores}";
            QuantidadeRedutoresTexto = $"Redutor: {totalRedutores}";

            OnPropertyChanged(nameof(TotalRetrabalhadosTexto));
            OnPropertyChanged(nameof(QuantidadeMotoresTexto));
            OnPropertyChanged(nameof(QuantidadeRedutoresTexto));

            // Atualiza o gráfico de pizza
            RedutoresPieSeries = contagemPorTipoRedutor.Select(kv =>
                new PieSeries<int>
                {
                    Name = kv.Key,
                    Values = new[] { kv.Value },
                    DataLabelsPaint = new SolidColorPaint(SKColors.White),
                    DataLabelsPosition = PolarLabelsPosition.Middle,
                    DataLabelsFormatter = point => $"{point.Context.Series.Name}: {point.PrimaryValue}"
                }).ToArray();

            QuantidadePorTipoRedutor.Clear();
            foreach (var kv in contagemPorTipoRedutor.OrderBy(k => k.Key))
                QuantidadePorTipoRedutor.Add($"Tipo {kv.Key}: {kv.Value} unidades");

            OnPropertyChanged(nameof(RedutoresPieSeries));
            OnPropertyChanged(nameof(QuantidadePorTipoRedutor));
        }




        private string? _mesSelecionado;
        public string? MesSelecionado
        {
            get => _mesSelecionado;
            set
            {
                _mesSelecionado = value;
                OnPropertyChanged();
                int mes = ObterNumeroMes(value ?? "Todos");
                CalcularMediaPorTipo(mes);

            }
        }

        private int ObterNumeroMes(string mes)
        {
            return mes switch
            {
                "Janeiro" => 1,
                "Fevereiro" => 2,
                "Março" => 3,
                "Abril" => 4,
                "Maio" => 5,
                "Junho" => 6,
                "Julho" => 7,
                "Agosto" => 8,
                "Setembro" => 9,
                "Outubro" => 10,
                "Novembro" => 11,
                "Dezembro" => 12,
                _ => 0 
            };
        }

        public void CalcularMediaPorTipo(int mes = 0)
        {
            try
            {
                using var conn = new MySqlConnection(_connectionString);
                conn.Open();

                string query = @"
                SELECT equipamento, TIMESTAMPDIFF(SECOND, data, data_finalizacao) AS segundos
                FROM controle_lotes
                WHERE data_finalizacao IS NOT NULL";

                if (mes > 0)
                {
                    query += " AND MONTH(data) = @mes";
                }

                using var cmd = new MySqlCommand(query, conn);

                if (mes > 0)
                    cmd.Parameters.AddWithValue("@mes", mes);

                using var reader = cmd.ExecuteReader();

                var tempoPorTipo = new Dictionary<string, double>();
                var contagemPorTipo = new Dictionary<string, int>();

                while (reader.Read())
                {
                    string equipamento = reader["equipamento"].ToString();
                    int segundos = Convert.ToInt32(reader["segundos"]);
                    string tipo = equipamento.Substring(0, 1).ToUpper();

                    if (tempoPorTipo.ContainsKey(tipo))
                    {
                        tempoPorTipo[tipo] += segundos;
                        contagemPorTipo[tipo] += 1;
                    }
                    else
                    {
                        tempoPorTipo[tipo] = segundos;
                        contagemPorTipo[tipo] = 1;
                    }
                }

                var sb = new StringBuilder();
                var series = new List<ISeries>();

                foreach (var kv in tempoPorTipo)
                {
                    double mediaSegundos = kv.Value / contagemPorTipo[kv.Key];
                    TimeSpan media = TimeSpan.FromSeconds(mediaSegundos);

                    sb.AppendLine($"Tipo {kv.Key}: {media.Days} dias, {media.Hours} horas, {media.Minutes} minutos");

                    series.Add(new PieSeries<double>
                    {
                        Name = $"Tipo {kv.Key}",
                        Values = new[] { media.TotalHours },
                        DataLabelsPaint = new SolidColorPaint(SKColors.White),
                        DataLabelsPosition = PolarLabelsPosition.Middle,
                        DataLabelsFormatter = point =>
                        {
                            TimeSpan ts = TimeSpan.FromHours(point.PrimaryValue);
                            return $"{ts.Days}d {ts.Hours}h";
                        }
                    });
                }

                MediaPorTipoTexto = sb.ToString();
                TempoMedioFinalização = series;

                OnPropertyChanged(nameof(MediaPorTipoTexto));
                OnPropertyChanged(nameof(TempoMedioFinalização));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao calcular média do tempo de retrabalho por tipo: {ex.Message}");
            }
        }


        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? nome = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nome));
    }
}
