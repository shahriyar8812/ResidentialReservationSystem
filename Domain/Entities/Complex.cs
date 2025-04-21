using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class Complex
    {
        public int Id { get; set; }
        public string Name { get; set; } // نام مجتمع
        public string Address { get; set; }

        public ICollection<Unit> Units { get; set; }
    }

}
