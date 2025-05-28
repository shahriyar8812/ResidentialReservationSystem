namespace Application.Dtos
{
    public class UnitEditDto
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public int ComplexId { get; set; }
        public string Description { get; set; }
        public int Capacity { get; set; }
        public int BedroomCount { get; set; }
    }
}