namespace DikidiStalker
{
    public class ServiceDataResponse
    {
        public ServiceData Data { get; set; }
    }

    public class ServiceData
    {
        public List<ServiceCategory> List { get; set; }

        public CurrencyInfo Currency { get; set; }

        public int MinimalCost { get; set; }
    }

    public class ServiceCategory
    {
        public string Id { get; set; }

        public string Name { get; set; }

        public string CategoryValue { get; set; }

        public int CategoryCustomSort { get; set; }

        public string FieldSortService { get; set; }

        public List<Service> Services { get; set; }
    }

    public class Service
    {
        public int Id { get; set; }

        public int CompanyServiceId { get; set; }

        public string Name { get; set; }

        public string Image { get; set; }

        public int Cost { get; set; }

        public int Time { get; set; }

        public bool Floating { get; set; }

        public int Discount { get; set; }

        public int Price { get; set; }

        public int Share { get; set; }

        public string ServiceValue { get; set; }

        public double ServicePoints { get; set; }

        public int ServiceCustomSort { get; set; }

        public IconInfo Icon { get; set; }
    }

    public class IconInfo
    {
        public long Id { get; set; }
        public string Url { get; set; }
    }

    public class CurrencyInfo
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Abbr { get; set; }
        public string Iso { get; set; }
    }
}
