namespace Business.DTOs
{
    public class PortDTO
    {
        public int Id { get; set; }
        public Guid? Identifier { get; set; }
        public string? Name { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public DateTime? CertificationDate { get; set; }
        public string TimeSlice { get; set; }
    }
}
