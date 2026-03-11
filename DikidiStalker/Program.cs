using DikidiStalker;
using DikidiStalker.Backup;
using DikidiStalker.Config;
using DikidiStalker.Models;

internal class Program
{
    private static ConfigurationManager _configuration;
    private static string _baseDirectory;
    private static BackupManager _backupManager;
    private const int _baseDelay = 60;

    private static void Main(string[] args)
    {
        _configuration = new ConfigurationManager();
        _baseDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DikidiCompanyes");
        _backupManager = new BackupManager(_baseDirectory);
        BackgroundWorker();
    }

    static void BackgroundWorker()
    {
        var now = DateTime.Now;

        Console.WriteLine($"[ {now} ] DikidiStalker started");

        var currentDikidiCompanyes = new List<DikidiCompany>();

        var currentDataInfo = new Dictionary<string, Dictionary<DateTime, DataInfoResponse>>();
        var currentServiceData = new Dictionary<string, ServiceDataResponse>();

        currentDataInfo = _backupManager.LoadBackup<Dictionary<string, Dictionary<DateTime, DataInfoResponse>>>("SlotBackUp");
        currentServiceData = _backupManager.LoadBackup<Dictionary<string, ServiceDataResponse>>("ServiceBackUp");

        var lastInfoUpdate = DateTime.MinValue;
        var lastServiceUpdate = DateTime.MinValue;

        while (true)
        {
            var config = _configuration.LoadConfiguration();

            var actualDikidiCompanyes = config.DikidiCompanyes;
            var dataInfoInhibitor = config.Application.DataInfoDelay;
            var serviceDataInhibitor = config.Application.ServiceDataDelay;
            var dataInfoPeriod = config.Application.DataInfoPeriod;

            InitializeDirectory(actualDikidiCompanyes);

            now = DateTime.Now;

            var totalInfoMinutes = (now - lastInfoUpdate).TotalMinutes;
            var totalServiceMinutes = (now - lastServiceUpdate).TotalMinutes;

            foreach (var company in actualDikidiCompanyes)
            {
                var isNew = !currentDikidiCompanyes.Exists(c => c.CompanyId == company.CompanyId);

                if (totalInfoMinutes > dataInfoInhibitor || isNew)
                {
                    CheckDikidiSlots(currentDataInfo, dataInfoPeriod, company);
                    _backupManager.SaveBackup(currentDataInfo, "SlotBackUp");
                }
                if (totalServiceMinutes > serviceDataInhibitor || isNew)
                {
                    CheckDikidiService(currentDataInfo, currentServiceData, company);
                    _backupManager.SaveBackup(currentServiceData, "ServiceBackUp");
                }

                Thread.Sleep(500);
            }

            var onlyInActual = actualDikidiCompanyes.Where(c => !currentDikidiCompanyes.Select(x => x.CompanyId).Contains(c.CompanyId)).ToList();

            currentDikidiCompanyes = actualDikidiCompanyes.ToList();

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

                    Console.WriteLine();
                }
                else if (onlyInActual.Count != 0)
                {
                    Console.WriteLine($"[ {DateTime.Now} ]\tАнализ всех данных по добавленным организациям: {onlyInActual.Count} шт.");
                }
            }

            Task.Delay(1000 * _baseDelay).Wait();
        }
    }

    private static void InitializeDirectory(List<DikidiCompany> DikidiCompanyes)
    {
        if (!Directory.Exists(_baseDirectory)) Directory.CreateDirectory(_baseDirectory);

        foreach (var dikidiCompanye in DikidiCompanyes)
        {
            var dikidiCompanyFolder = Path.Combine(_baseDirectory, $"Dikidi_{dikidiCompanye.CompanyId}-{dikidiCompanye.ServiceId}");

            if (!Directory.Exists(dikidiCompanyFolder))
            {
                Console.WriteLine($"[ {DateTime.Now} ]\tСоздание папки для организации {dikidiCompanye.CompanyId}");
                Directory.CreateDirectory(dikidiCompanyFolder);
            }

            var infoFile = Path.Combine(dikidiCompanyFolder, "Info.dkdstlk");
            var serviceFile = Path.Combine(dikidiCompanyFolder, "Service.dkdstlk");

            if (!File.Exists(infoFile))
                File.WriteAllText(infoFile, string.Empty);

            if (!File.Exists(serviceFile))
                File.WriteAllText(serviceFile, string.Empty);
        }
    }


    private static void CheckDikidiSlots(Dictionary<string, Dictionary<DateTime, DataInfoResponse>> currentDataInfo, int dataInfoPeriod, DikidiCompany company)
    {
        var now = DateTime.Now.Date;
        var filePath = Path.Combine(_baseDirectory, $"Dikidi_{company.CompanyId}-{company.ServiceId}", "Info.dkdstlk");

        var actualDataInfo = Enumerable.Range(0, dataInfoPeriod)
            .Select(day => now.AddDays(day))
            .Select(date => new
            {
                Date = date,
                Slots = DikidiInfo.GetDikidiSlots(company, date)
            })
            .Where(x => x.Slots != null)
            .ToDictionary(x => x.Date, x =>
            {
                var filteredTimes = x.Slots.Data.Times
                .Select(t => new
                {
                    t.Key,
                    Times = t.Value
                    .Where(el => DateTime.Parse(el).Date == x.Date)
                    .ToList()
                })
                .Where(t => t.Times.Count > 0)
                .ToDictionary(t => t.Key, t => t.Times);
                if (filteredTimes.Count == 0)
                    return null;
                x.Slots.Data.Times = filteredTimes;
                return x.Slots;
            })
            .Where(x => x.Value != null)
            .ToDictionary(x => x.Key, x => x.Value);

        var masters = actualDataInfo.Values
            .SelectMany(a => a.Data.Masters)
            .DistinctBy(x => x.Key)
            .ToDictionary(x => x.Key, x => x.Value);

        var slotUpdate = new SlotUpdate();

        if (currentDataInfo.TryGetValue(company.CompanyId, out var current))
        {
            slotUpdate = UpdateCurrentSlots(current, actualDataInfo);
        }
        else
        {
            slotUpdate.InitializeCollection = actualDataInfo;
        }

        var companyInfo = actualDataInfo.Values.FirstOrDefault()?.Data?.Company;

        SlotManager.Print(masters, companyInfo, slotUpdate, filePath);

        currentDataInfo[company.CompanyId] = actualDataInfo;
    }

    private static SlotUpdate UpdateCurrentSlots(Dictionary<DateTime, DataInfoResponse> currentDataInfo, Dictionary<DateTime, DataInfoResponse> actualDataInfo)
    {
        var now = DateTime.Now;
        var slotUpdate = new SlotUpdate();
        var minActual = actualDataInfo.Keys.Min();

        //currentDataInfo.Remove(currentDataInfo.Last().Key);

        var maxCurrent = currentDataInfo.Keys.Max();

        if (actualDataInfo.Count < currentDataInfo.Count)
        {
            maxCurrent = actualDataInfo.Keys.Max();
        }

        //actualDataInfo.First().Value.Data.Times.Values.First().RemoveAt(1);
        //currentDataInfo.Last().Value.Data.Times.Values.First().RemoveAt(1);

        var currentCollection = currentDataInfo.Where(c => c.Key >= minActual && c.Key <= maxCurrent).ToList();
        var actualCollection = actualDataInfo.Where(c => c.Key >= minActual && c.Key <= maxCurrent).ToList();
        var newCollection = actualDataInfo.Where(c => c.Key > maxCurrent).ToDictionary();

        for (var day = minActual; day <= maxCurrent; day = day.AddDays(1))
        {
            var divCurrent = currentCollection.FirstOrDefault(c => c.Key == day).Value?.Data;
            var divActual = actualCollection.FirstOrDefault(c => c.Key == day).Value?.Data;

            var delCollection = new Dictionary<string, List<string>>();
            var addCollection = new Dictionary<string, List<string>>();

            if (divCurrent is null || divActual is null) 
                continue;

            foreach (var time in divCurrent?.Times?.Where(t => t.Key != "0"))
            {
                if (!delCollection.ContainsKey(time.Key))
                    delCollection[time.Key] = new List<string>();

                delCollection[time.Key].AddRange(time.Value.Where(value => !divActual.Times[time.Key].Contains(value)).ToList());

                if (delCollection[time.Key].Count == 0)
                    delCollection.Remove(time.Key);
            }

            foreach (var time in divActual?.Times?.Where(t => t.Key != "0"))
            {
                if (!addCollection.ContainsKey(time.Key))
                    addCollection[time.Key] = new List<string>();

                addCollection[time.Key].AddRange(time.Value.Where(value => !divCurrent.Times[time.Key].Contains(value)).ToList());

                if (addCollection[time.Key].Count == 0)
                    addCollection.Remove(time.Key);
            }

            if (delCollection.Count != 0) slotUpdate.DelCollection[day] = delCollection;
            if (addCollection.Count != 0) slotUpdate.AddCollection[day] = addCollection;
        }

        slotUpdate.NewCollection = newCollection;

        return slotUpdate;
    }


    private static void CheckDikidiService(Dictionary<string, Dictionary<DateTime, DataInfoResponse>> currentDataInfo, Dictionary<string, ServiceDataResponse> currentServiceData, DikidiCompany company)
    {
        var filePath = Path.Combine(_baseDirectory, $"Dikidi_{company.CompanyId}-{company.ServiceId}", "Service.dkdstlk");
        var actualServiceData = DikidiInfo.GetServices(company);
        var companyInfo = currentDataInfo.FirstOrDefault(i => i.Key == company.CompanyId.ToString()).Value?.FirstOrDefault().Value?.Data?.Company;

        var masters = currentDataInfo
            .SelectMany(outerKvp => outerKvp.Value)
            .SelectMany(innerKvp => innerKvp.Value.Data.Masters)
            .GroupBy(m => m.Key)
            .ToDictionary(g => g.Key, g => g.Last().Value);

        if (companyInfo is null || actualServiceData is null) return;

        var serviceUpdate = new ServiceUpdate();

        if (currentServiceData.TryGetValue(company.CompanyId, out var current))
        {
            serviceUpdate = UpdateCurrentService(companyInfo, current, actualServiceData);
        }
        else
        {
            serviceUpdate.InitializeCollection = actualServiceData;
        }

        ServiceManager.Print(masters, companyInfo, actualServiceData, serviceUpdate, filePath);

        currentServiceData[company.CompanyId] = actualServiceData;
    }

    private static ServiceUpdate UpdateCurrentService(CompanyInfo companyInfo, ServiceDataResponse currentServiceData, ServiceDataResponse actualServiceData)
    {
        var serviceUpdate = new ServiceUpdate();
        var currentService = currentServiceData.Data.List.ToList();
        var actualService = actualServiceData.Data.List.ToList();

        //currentService.RemoveAt(currentService.Count-1);
        //actualService.RemoveAt(0);

        var actualBlockIds = new HashSet<string>(actualService.Select(s => s.Id));
        var currentBlockIds = new HashSet<string>(currentService.Select(s => s.Id));

        var modBlockCollection = currentService.Where(s => actualBlockIds.Contains(s.Id));

        serviceUpdate.DelBlockCollection = currentService.Where(s => !actualBlockIds.Contains(s.Id)).ToList();
        serviceUpdate.AddBlockCollection = actualService.Where(s => !currentBlockIds.Contains(s.Id)).ToList();

        if (modBlockCollection.Count() != 0)
        {
            foreach (var block in modBlockCollection)
            {
                var currentServiceIds = currentService.First(s => s.Id == block.Id).Services.Select(s => s.Id.ToString()).ToList();
                var actualServiceIds = actualService.First(s => s.Id == block.Id).Services.Select(s => s.Id.ToString()).ToList();

                //currentServiceIds.RemoveAt(currentServiceIds.Count - 1);
                //actualServiceIds.RemoveAt(0);

                var delServiceCollection = currentService.First(s => s.Id == block.Id).Services.Where(s => !actualServiceIds.Contains(s.Id.ToString())).ToList();
                var addServiceCollection = actualService.First(s => s.Id == block.Id).Services.Where(s => !currentServiceIds.Contains(s.Id.ToString())).ToList();

                var modServiceCollection = currentService.First(s => s.Id == block.Id).Services.Where(s => actualServiceIds.Contains(s.Id.ToString())).ToList();

                if (delServiceCollection.Count != 0) serviceUpdate.DelServiceCollection[block.Id] = delServiceCollection;
                if (addServiceCollection.Count != 0) serviceUpdate.AddServiceCollection[block.Id] = addServiceCollection;

                if (modServiceCollection.Count != 0)
                {
                    foreach (var el in modServiceCollection)
                    {
                        var curService = currentService.First(s => s.Id == block.Id).Services.First(s => s.Id == el.Id);
                        var actlService = actualService.First(s => s.Id == block.Id).Services.First(s => s.Id == el.Id);

                        //actlService.Price = 100;
                        //actlService.Name = "123";
                        //actlService.Time = 10;

                        var isPrise = curService.Price != actlService.Price;
                        var isName = curService.Name != actlService.Name;
                        var isTime = curService.Time != actlService.Time;

                        if (isPrise || isName || isTime)
                        {
                            if (!serviceUpdate.ModServiceCollection.ContainsKey(block.Id))
                            {
                                serviceUpdate.ModServiceCollection[block.Id] = new Dictionary<int, (Service, Service)>();
                            }
                            serviceUpdate.ModServiceCollection[block.Id][el.Id] = (curService, actlService);
                        }

                        var name = isName ? curService.Id.ToString() : curService.Name.Replace('\n', ' ');
                    }
                }
            }
        }

        return serviceUpdate;
    }
}