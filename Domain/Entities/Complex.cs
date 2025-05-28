using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Domain.Entities
{
    public class Complex
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }

        [BindNever]
        public ICollection<Unit> Units { get; set; }
    }
}