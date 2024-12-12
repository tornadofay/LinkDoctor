namespace LinkDoctor.src.Models
{
    public class EndpointConfiguration
    {
        public string Name { get; set; } = "";
        public string Address { get; set; } = "";
        public bool IsDomain { get; set; }
        public int Priority { get; set; }
    }
}
