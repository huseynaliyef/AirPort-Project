namespace Business.DTOs
{
    public class PortSearchDTO
    {
        public DateTime EffectiveDate {  get; set; }
        public States State { get; set; }
    }

    public enum States
    {
        BASELINE,
        SNAPSHOT
    }
}
