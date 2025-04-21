using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class UnitImage
    {
        public int Id { get; set; }
        public string ImageUrl { get; set; }

        public int UnitId { get; set; }
        public Unit Unit { get; set; }
    }

}
