namespace DikidiStalker
{
    public class DataInfoResponse
    {
        public DataInfo? Data { get; set; }
    }

    public class DataInfo
    {
        public CompanyInfo? Company { get; set; }
        public Dictionary<string, MasterInfo>? Masters { get; set; }
        public List<string>? DatesTrue { get; set; }
        public string? DateNear { get; set; }
        public Dictionary<string, List<string>>? Times { get; set; }
        public string? FirstDateTrue { get; set; }
        public string? Warning { get; set; }
    }

    public class CompanyInfo
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? Image { get; set; }
        public List<string>? Phones { get; set; }
        public string? Phone { get; set; }
        public string? Address { get; set; }
        public string? Master { get; set; }
        public string? StatusId { get; set; }
        public string? CurrencyAbbr { get; set; }
    }

    public class MasterInfo
    {
        public string? Id { get; set; }
        public string? Username { get; set; }
        public string? Image { get; set; }
        public string? ServiceName { get; set; }
        public string? Count { get; set; }
        public string? Rating { get; set; }
        public string? Cost { get; set; }
        public string? Time { get; set; }
        public string? Price { get; set; }
    }
}
