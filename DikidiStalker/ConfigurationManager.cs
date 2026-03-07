using System.Xml.Serialization;

namespace DikidiStalker
{
    public class ConfigurationManager
    {
        private readonly string _configFilePath;
        private readonly string _configDirectory;

        public ConfigurationManager(string configFilePath = null)
        {
            _configFilePath = configFilePath ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "AppConfig.xml");
            _configDirectory = Path.GetDirectoryName(_configFilePath);
        }

        public AppConfiguration LoadConfiguration()
        {
            try
            {
                if (!Directory.Exists(_configDirectory))
                    Directory.CreateDirectory(_configDirectory);

                if (!File.Exists(_configFilePath))
                    return CreateDefaultConfiguration();

                return ReadConfigurationFromFile();
            }
            catch
            {
                return AppConfiguration.GetDefault();
            }
        }

        private AppConfiguration CreateDefaultConfiguration()
        {
            var defaultConfig = AppConfiguration.GetDefault();

            SaveConfiguration(defaultConfig);

            return defaultConfig;
        }

        private AppConfiguration ReadConfigurationFromFile()
        {
            try
            {
                var serializer = new XmlSerializer(typeof(AppConfiguration));

                using (var reader = new StreamReader(_configFilePath))
                {
                    var res = (AppConfiguration)serializer.Deserialize(reader);

                    ValidateConfig(res);

                    return res;
                }
            }
            catch
            {
                return CreateDefaultConfiguration();
            }
        }

        private void ValidateConfig(AppConfiguration res)
        {
            res.Application.ServiceDataDelay = res.Application.ServiceDataDelay < 1
                ? 1
                : res.Application.ServiceDataDelay;
            res.Application.DataInfoDelay = res.Application.DataInfoDelay < 1
                ? 1
                : res.Application.DataInfoDelay;
            res.Application.DataInfoPeriod = res.Application.DataInfoPeriod < 1
                ? 1
                : res.Application.DataInfoPeriod;
            res.DikidiCompanyes = res.DikidiCompanyes.Where(c => c.CompanyId != "" || c.ServiceId != "").Distinct().ToList();
        }

        public void SaveConfiguration(AppConfiguration config)
        {
            try
            {
                var serializer = new XmlSerializer(typeof(AppConfiguration));

                var settings = new System.Xml.XmlWriterSettings
                {
                    Indent = true,
                    IndentChars = "  ",
                    NewLineOnAttributes = false,
                    Encoding = System.Text.Encoding.UTF8
                };

                using (var writer = System.Xml.XmlWriter.Create(_configFilePath, settings))
                {
                    serializer.Serialize(writer, config);
                }
            }
            catch { }
        }
    }
}
