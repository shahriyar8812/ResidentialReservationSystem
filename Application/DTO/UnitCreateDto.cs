using System.Collections.Generic;

namespace Application.Dtos
{
    public class UnitCreateDto
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public int ComplexId { get; set; }
        public string Description { get; set; }
        public int Capacity { get; set; }
        public int BedroomCount { get; set; }
        public bool IsAvailable { get; set; }

        public List<UnitImageDto> UnitImages { get; set; } = new List<UnitImageDto>();
        public List<UnitFeatureDto> UnitFeatures { get; set; } = new List<UnitFeatureDto>();
        public RateDto Rate { get; set; }
    }
}