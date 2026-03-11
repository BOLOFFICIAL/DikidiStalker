using DikidiStalker.Config;

namespace DikidiStalker.Models
{
    public class DikidiRequest
    {
        public DikidiServiceRequest DikidiService { get; set; }
        public DikidiTimeRequest DikidiTime { get; set; }
        public DikidiRequest(DikidiCompany company, DateTime date)
        {
            DikidiTime = new DikidiTimeRequest()
            {
                RequestUri = $"https://dikidi.ru/ru/mobile/ajax/newrecord/get_datetimes/?company_id={company.CompanyId}&date={date.ToString("yyyy-MM-dd")}&service_id%5B%5D={company.ServiceId}&with_first=false&day_month=",
                Referer = $"https://dikidi.ru/ru/record/{company.CompanyId}?p=3.pi-po-ssm-sd&o=7&s={company.ServiceId}&rl=0_undefined&source=widget",
            };
            DikidiService = new DikidiServiceRequest()
            {

                RequestUri = $"https://dikidi.ru/mobile/ajax/newrecord/company_services/?lang=ru&array=1&company={company.CompanyId}&master=&share=",
                Referer = $"https://dikidi.app/{company.CompanyId}?p=1.pi-po",
            };
        }
    }

    public class DikidiServiceRequest
    {
        public string RequestUri { get; set; }
        public string Referer { get; set; }
    }

    public class DikidiTimeRequest
    {
        public string RequestUri { get; set; }
        public string Referer { get; set; }
    }
}
