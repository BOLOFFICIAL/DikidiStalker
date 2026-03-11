using System.Xml.Serialization;

namespace DikidiStalker.Config
{
    [Serializable]
    [XmlRoot("Configuration")]
    public class AppConfiguration
    {
        [XmlElement("Application")]
        public ApplicationSettings Application { get; set; }

        [XmlArray("DikidiCompany")]
        [XmlArrayItem("DikidiCompanyes")]
        public List<DikidiCompany> DikidiCompanyes { get; set; }

        public AppConfiguration()
        {
            Application = new ApplicationSettings
            {
                ServiceDataDelay = 1 * 60 * 24,
                DataInfoDelay = 1,
                DataInfoPeriod = 3
            };
            DikidiCompanyes = new List<DikidiCompany>();
        }

        public static AppConfiguration GetDefault()
        {
            return new AppConfiguration
            {
                Application = new ApplicationSettings
                {
                    ServiceDataDelay = 1 * 60 * 24,
                    DataInfoDelay = 1,
                    DataInfoPeriod = 3
                },

                DikidiCompanyes = new List<DikidiCompany>()
                {
                    new DikidiCompany(){ CompanyId = "", ServiceId = "" },
                    new DikidiCompany(){ CompanyId = "", ServiceId = "" },
                    new DikidiCompany(){ CompanyId = "", ServiceId = "" },
                }
            };
        }
    }

    [Serializable]
    public class ApplicationSettings
    {
        [XmlElement("ServiceDataDelay")]
        public int ServiceDataDelay { get; set; }

        [XmlElement("DataInfoDelay")]
        public int DataInfoDelay { get; set; }

        [XmlElement("DataInfoPeriod")]
        public int DataInfoPeriod { get; set; }
    }

    [Serializable]
    public class DikidiCompany
    {
        [XmlAttribute("CompanyId")]
        public string CompanyId { get; set; }

        [XmlAttribute("ServiceId")]
        public string ServiceId { get; set; }
    }
}
