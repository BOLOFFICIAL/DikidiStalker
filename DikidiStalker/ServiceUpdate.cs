namespace DikidiStalker
{
    public class ServiceUpdate
    {
        public ServiceDataResponse InitializeCollection { get; set; }
        public List<ServiceCategory> AddBlockCollection { get; set; } = new();
        public List<ServiceCategory> DelBlockCollection { get; set; } = new();
        public Dictionary<string, List<Service>> DelServiceCollection { get; set; } = new();
        public Dictionary<string, List<Service>> AddServiceCollection { get; set; } = new();
        public Dictionary<string, Dictionary<int, (Service, Service)>> ModServiceCollection { get; set; } = new();
        public string Exception { get; set; }
    }
}
