using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class Unit
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public int ComplexId { get; set; }
        public string Description { get; set; }

        public int Capacity { get; set; }
        public int BedroomCount { get; set; }
        public bool IsAvailable { get; set; }

        public List<Facility> Features { get; set; }


        public bool HasMandatoryCheckInOut { get; set; }



        public Complex Complex { get; set; }
        public ICollection<UnitImage> UnitImages { get; set; }
        public ICollection<UnitFeature> UnitFeatures { get; set; }
        public ICollection<Rate> Rates { get; set; }
        public ICollection<Reservation> Reservations { get; set; }
    }

}
