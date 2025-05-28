namespace Application.Dtos
{
    public class RateDto
    {
        public int Id { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal PricePerNight { get; set; }
        public int UnitId { get; set; }
    }
}