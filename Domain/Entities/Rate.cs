using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class Rate
    {
        public int Id { get; set; }
        public int UnitId { get; set; }
        public Unit Unit { get; set; }

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        public decimal PricePerNight { get; set; }
    }

}
