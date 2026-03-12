using DikidiStalker.Config;
using DikidiStalker.Models;
using Newtonsoft.Json;

namespace DikidiStalker
{
    public class DikidiInfo
    {
        public static ServiceDataResponse GetCompanyServices(DikidiCompany company)
        {
            try
            {
                var requestUri = $"https://dikidi.ru/mobile/ajax/newrecord/company_services/?lang=ru&array=1&company={company.CompanyId}&master=&share=";
                var referer = $"https://dikidi.app/{company.CompanyId}?p=1.pi-po";

                return SendRequest<ServiceDataResponse>(requestUri, referer).Result;
            }
            catch
            {
                return null;
            }
        }

        public static DataInfoResponse GetAvailableTimeSlots(DikidiCompany company, DateTime date)
        {
            try
            {
                var requestUri = $"https://dikidi.ru/ru/mobile/ajax/newrecord/get_datetimes/?company_id={company.CompanyId}&date={date.ToString("yyyy-MM-dd")}&service_id%5B%5D={company.ServiceId}&with_first=false&day_month=";
                var referer = $"https://dikidi.ru/ru/record/{company.CompanyId}?p=3.pi-po-ssm-sd&o=7&s={company.ServiceId}&rl=0_undefined&source=widget";

                return SendRequest<DataInfoResponse>(requestUri, referer).Result;
            }
            catch
            {
                return null;
            }
        }

        private static async Task<T> SendRequest<T>(string RequestUri, string Referer)
        {
            try
            {
                using var client = new HttpClient();

                var request = new HttpRequestMessage
                {
                    Method = HttpMethod.Get,
                    RequestUri = new Uri(RequestUri)
                };

                request.Headers.Add("Accept", "application/json, text/javascript, */*; q=0.01");
                request.Headers.Add("X-Requested-With", "XMLHttpRequest");
                request.Headers.Add("Referer", Referer);

                var response = client.SendAsync(request).Result;
                response.EnsureSuccessStatusCode();

                string responseBody = await response.Content.ReadAsStringAsync();

                return JsonConvert.DeserializeObject<T>(responseBody);
            }
            catch (Exception ex)
            {
                return default;
            }
        }
    }
}
