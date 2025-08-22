using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EquipamentosRetrabalho.Model
{
    public class EquipamentosModel
    {
        public string? OrdemMontagem { get; set; }
        public string? OrdemVenda { get; set; }
        public string? Cliente { get; set; }
        public string? ItemVenda { get; set; }
        public string? EquipamentoNome { get; set; }
        public int? QuantidadeTotal { get; set; }
        public int? Reprovado { get; set; }
        public DateTime? Data { get; set; }
        public string? Defeito { get; set; }
        public string? Status { get; set; }
        public string? Local { get; set; }
        public DateTime? DataFinalizacao { get; set; }

    }
}
