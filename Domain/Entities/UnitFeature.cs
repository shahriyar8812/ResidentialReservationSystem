using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class UnitFeature
    {
        public int Id { get; set; }

        public int UnitId { get; set; }
        public Unit Unit { get; set; }

        public int FeatureId { get; set; }
        public Feature Feature { get; set; }
    }

}
