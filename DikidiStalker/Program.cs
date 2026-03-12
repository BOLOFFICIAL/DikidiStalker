using DikidiStalker;
using DikidiStalker.Config;

internal class Program
{
    private static ConfigurationManager _configuration;
    private static SlotManager _slotManager;
    private static ServiceManager _serviceManager;
    private const int _baseDelay = 60;

    private static void Main(string[] args)
    {
        _configuration = new ConfigurationManager();
        var baseDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DikidiCompanyes");
        _slotManager = new SlotManager(baseDirectory);
        _serviceManager = new ServiceManager(baseDirectory);
        BackgroundWorker();
    }

    static void BackgroundWorker()
    {
        var currentDikidiCompanyes = new List<DikidiCompany>();

        var currentDataInfo = _slotManager.LoadBackup();
        var currentServiceData = _serviceManager.LoadBackup();

        var lastInfoUpdate = DateTime.MinValue;
        var lastServiceUpdate = DateTime.MinValue;

        var now = DateTime.Now;

        Console.WriteLine($"[ {now} ]\tDikidiStalker started");

        while (true)
        {
            now = DateTime.Now;

            var config = _configuration.LoadConfiguration();

            var actualDikidiCompanyes = config.DikidiCompanyes;
            var dataInfoInhibitor = config.Application.DataInfoDelay;
            var serviceDataInhibitor = config.Application.ServiceDataDelay;
            var dataInfoPeriod = config.Application.DataInfoPeriod;

            var totalInfoMinutes = (now - lastInfoUpdate).TotalMinutes;
            var totalServiceMinutes = (now - lastServiceUpdate).TotalMinutes;

            foreach (var company in actualDikidiCompanyes)
            {
                var isNew = !currentDikidiCompanyes.Exists(c => c.CompanyId == company.CompanyId);

                if (totalInfoMinutes > dataInfoInhibitor || isNew)
                {
                    _slotManager.CheckDikidiSlots(currentDataInfo, dataInfoPeriod, company);
                }
                if (totalServiceMinutes > serviceDataInhibitor || isNew)
                {
                    _serviceManager.CheckDikidiService(currentDataInfo, currentServiceData, company);
                }

                Thread.Sleep(500);
            }

            var onlyInActual = actualDikidiCompanyes.Where(c => !currentDikidiCompanyes.Select(x => x.CompanyId).Contains(c.CompanyId)).ToList();

            if (totalInfoMinutes > dataInfoInhibitor || totalServiceMinutes > serviceDataInhibitor || onlyInActual.Count != 0)
            {
                if (totalInfoMinutes > dataInfoInhibitor || totalServiceMinutes > serviceDataInhibitor)
                {
                    Console.Write($"[ {DateTime.Now} ]\tАнализ завершен: ");

                    if (totalInfoMinutes > dataInfoInhibitor)
                    {
                        lastInfoUpdate = now;
                        Console.Write($"( Слоты ) ");
                    }
                    if (totalServiceMinutes > serviceDataInhibitor)
                    {
                        lastServiceUpdate = now;
                        Console.Write($"( Услуги ) ");
                    }

                    if (currentDikidiCompanyes.Count > 0 && onlyInActual.Count != 0)
                    {
                        Console.Write($"( Данные по новым ораганизациям ) ");
                    }

                    Console.WriteLine();
                }
                else if (onlyInActual.Count != 0)
                {
                    Console.WriteLine($"[ {DateTime.Now} ]\tАнализ всех данных по добавленным организациям");
                }
            }

            currentDikidiCompanyes = actualDikidiCompanyes.ToList();

            var delaySeconds = Math.Min((int)(DateTime.Now - now).TotalSeconds, _baseDelay);

            Task.Delay(1000 * (_baseDelay - delaySeconds)).Wait();
        }
    }
}