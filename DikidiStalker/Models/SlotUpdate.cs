namespace DikidiStalker.Models
{
    public class SlotUpdate
    {
        public Dictionary<DateTime, DataInfoResponse> NewCollection { get; set; } = new();
        public Dictionary<DateTime, Dictionary<string, List<string>>> DelCollection { get; set; } = new();
        public Dictionary<DateTime, Dictionary<string, List<string>>> AddCollection { get; set; } = new();
        public Dictionary<DateTime, DataInfoResponse> InitializeCollection { get; set; } = new();
        public string Exception { get; set; }
    }
}
