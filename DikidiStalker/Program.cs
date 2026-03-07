using DikidiStalker;
using System.Text;

internal class Program
{
    private static ConfigurationManager _configuration;

    private static void Main(string[] args)
    {
        _configuration = new ConfigurationManager();
        BackgroundWorker();
    }

    static void BackgroundWorker()
    {
        Console.WriteLine("DikidiStalker started");

        var dikidiCompanyes = new List<DikidiCompany>();

        var currentDataInfo = new Dictionary<string, Dictionary<DateTime, DataInfoResponse>>();
        var currentServiceData = new Dictionary<string, ServiceDataResponse>();

        var lastInfoUpdate = DateTime.MinValue;
        var lastServiceUpdate = DateTime.MinValue;

        while (true)
        {
            var config = _configuration.LoadConfiguration();

            dikidiCompanyes = config.DikidiCompanyes;
            var dataInfoInhibitor = config.Application.DataInfoDelay;
            var serviceDataInhibitor = config.Application.ServiceDataDelay;
            var dataInfoPeriod = config.Application.DataInfoPeriod;

            InitializeDirectory(dikidiCompanyes);

            var now = DateTime.Now;

            var totalInfoMinutes = (now - lastInfoUpdate).TotalMinutes;
            var totalServiceMinutes = (now - lastServiceUpdate).TotalMinutes;

            foreach (var company in dikidiCompanyes)
            {
                if (totalInfoMinutes > dataInfoInhibitor)
                {
                    var actualDataInfo = new Dictionary<DateTime, DataInfoResponse>();

                    for (int day = 0; day < dataInfoPeriod; day++)
                    {
                        var currentDate = now.AddDays(day).Date;
                        var slots = DikidiInfo.GetDikidiSlots(company, currentDate);

                        if (slots is null) continue;

                        actualDataInfo.Add(currentDate, slots);
                    }

                    UpdateDataInfo(company, currentDataInfo, actualDataInfo);
                }

                if (totalServiceMinutes > serviceDataInhibitor)
                {
                    var actualServiceData = DikidiInfo.GetServices(company);
                    var com = currentDataInfo.FirstOrDefault(i => i.Key == company.CompanyId.ToString()).Value?.FirstOrDefault().Value?.Data?.Company;

                    UpdateServiceData(company, com, currentServiceData, actualServiceData);
                }
            }

            lastInfoUpdate = now;

            Console.WriteLine($"[ {DateTime.Now} ]\tДанные обновлены");

            Task.Delay(1000 * 60).Wait();
        }
    }

    private static void InitializeDirectory(List<DikidiCompany> DikidiCompanyes)
    {
        var appDirectory = AppDomain.CurrentDomain.BaseDirectory;

        var baseFolder = Path.Combine(appDirectory, "DikidiCompanyes");

        if (!Directory.Exists(baseFolder)) Directory.CreateDirectory(baseFolder);

        foreach (var dikidiCompanye in DikidiCompanyes)
        {
            var dikidiCompanyFolder = Path.Combine(baseFolder, $"Dikidi_{dikidiCompanye.CompanyId}-{dikidiCompanye.ServiceId}");
            if (!Directory.Exists(dikidiCompanyFolder))
                Directory.CreateDirectory(dikidiCompanyFolder);

            var infoFile = Path.Combine(dikidiCompanyFolder, "Info.dkdstlk");
            var serviceFile = Path.Combine(dikidiCompanyFolder, "Service.dkdstlk");

            if (!File.Exists(infoFile))
                File.WriteAllText(infoFile, string.Empty);

            if (!File.Exists(serviceFile))
                File.WriteAllText(serviceFile, string.Empty);
        }
    }

    static void UpdateDataInfo(DikidiCompany company, Dictionary<string, Dictionary<DateTime, DataInfoResponse>> currentDataInfo, Dictionary<DateTime, DataInfoResponse> actualDataInfo)
    {
        var companyInfo = actualDataInfo.Values.FirstOrDefault()?.Data?.Company;

        if (companyInfo is null) return;

        var infoFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DikidiCompanyes", $"Dikidi_{company.CompanyId}-{company.ServiceId}", "Info.dkdstlk");
        var content = new StringBuilder();
        var now = DateTime.Now;

        try
        {
            if (currentDataInfo.ContainsKey(companyInfo.Id))
            {
                var minActual = actualDataInfo.Keys.Min();
                var maxCurrent = currentDataInfo[companyInfo.Id].Keys.Max();

                if (actualDataInfo.Count < currentDataInfo[companyInfo.Id].Count)
                {
                    maxCurrent = actualDataInfo.Keys.Max();
                }

                var currentCollection = currentDataInfo[companyInfo.Id].Where(c => c.Key >= minActual && c.Key <= maxCurrent).ToList();
                var actualCollection = actualDataInfo.Where(c => c.Key >= minActual && c.Key <= maxCurrent).ToList();
                var modCollection = actualDataInfo.Where(c => c.Key > maxCurrent).ToList();

                for (var day = minActual; day <= maxCurrent; day = day.AddDays(1))
                {
                    var divCurrent = currentCollection.FirstOrDefault(c => c.Key == day).Value?.Data;
                    var divActual = actualCollection.FirstOrDefault(c => c.Key == day).Value?.Data;

                    var delCollection = new Dictionary<string, List<string>>();
                    var addCollection = new Dictionary<string, List<string>>();

                    foreach (var time in divCurrent.Times.Where(t => t.Key != "0"))
                    {
                        if (!delCollection.ContainsKey(time.Key))
                            delCollection[time.Key] = new List<string>();

                        delCollection[time.Key].AddRange(time.Value.Where(value => !divActual.Times[time.Key].Contains(value)).ToList());

                        if (delCollection[time.Key].Count == 0)
                            delCollection.Remove(time.Key);
                    }

                    foreach (var time in divActual.Times.Where(t => t.Key != "0"))
                    {
                        if (!addCollection.ContainsKey(time.Key))
                            addCollection[time.Key] = new List<string>();

                        addCollection[time.Key].AddRange(time.Value.Where(value => !divCurrent.Times[time.Key].Contains(value)).ToList());

                        if (addCollection[time.Key].Count == 0)
                            addCollection.Remove(time.Key);
                    }

                    if (delCollection.Any() || addCollection.Any())
                    {
                        var message = $"[ {now} ] Обнаружены изменения в окошках организации {companyInfo.Name}\n";

                        Console.Write(message);
                        content.AppendLine(message);
                    }

                    if (delCollection.Any())
                    {
                        content.AppendLine($"\t| Удаленные окошки {day.ToShortDateString()}:\n");

                        foreach (var del in delCollection)
                        {
                            content.AppendLine($"\t\t> {divCurrent.Masters[del.Key].Username}\n");

                            foreach (var value in del.Value)
                            {
                                content.AppendLine($"\t\t\t[ {DateTime.Parse(value).TimeOfDay} ]");
                            }
                            content.AppendLine();
                        }
                    }

                    if (addCollection.Any())
                    {
                        content.AppendLine($"\t| Добавленные окошки {day.ToShortDateString()}:\n");

                        foreach (var del in addCollection)
                        {
                            content.AppendLine($"\t\t> {divActual.Masters[del.Key].Username}\n");

                            foreach (var value in del.Value)
                            {
                                content.AppendLine($"\t\t\t[ {DateTime.Parse(value).TimeOfDay} ]");
                            }
                            content.AppendLine();
                        }
                    }
                }

                if (modCollection.Any())
                {
                    content.AppendLine($"[ {now} ] Обнаружены новые окошки:\n");

                    foreach (var mod in modCollection)
                    {
                        content.AppendLine($"\t| Дата: {mod.Key.ToShortDateString()}\n");

                        foreach (var time in mod.Value.Data.Times.Where(t => t.Key != "0"))
                        {
                            content.AppendLine($"\t\t> {mod.Value.Data?.Masters[time.Key].Username}\n");

                            foreach (var el in time.Value)
                            {
                                content.AppendLine($"\t\t\t[ {DateTime.Parse(el).TimeOfDay} ]");
                            }
                            content.AppendLine();
                        }
                    }
                }
            }
            else
            {
                content.AppendLine($"[ {now} ] Актуальные окошки для организации: {companyInfo.Name}\n");

                foreach (var dataInfo in actualDataInfo)
                {
                    content.AppendLine($"\t| Дата: {dataInfo.Key.ToShortDateString()}\n");

                    foreach (var time in dataInfo.Value.Data.Times.Where(t => t.Key != "0"))
                    {
                        content.AppendLine($"\t\t> {dataInfo.Value.Data?.Masters[time.Key].Username}\n");

                        foreach (var el in time.Value)
                        {
                            content.AppendLine($"\t\t\t[ {DateTime.Parse(el).TimeOfDay} ]");
                        }
                        content.AppendLine();
                    }
                }
            }
        }
        catch (Exception ex)
        {
            content.AppendLine($"[ {now} ] Возникла ошибка при обновлении окошек: {ex.Message}");
        }
        finally
        {
            using (StreamWriter writer = new StreamWriter(infoFile, append: true))
            {
                if (content.Length > 0)
                {
                    writer.Write(content);
                    writer.WriteLine("==================================================\n");
                }
            }

            currentDataInfo[companyInfo.Id] = actualDataInfo;
        }

    }

    static void UpdateServiceData(DikidiCompany company, CompanyInfo companyInfo, Dictionary<string, ServiceDataResponse> currentServiceData, ServiceDataResponse actualServiceData)
    {
        if (companyInfo is null) return;

        var serviceFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DikidiCompanyes", $"Dikidi_{company.CompanyId}-{company.ServiceId}", "Service.dkdstlk");
        var content = new StringBuilder();
        var now = DateTime.Now;

        try
        {
            if (currentServiceData.ContainsKey(companyInfo.Id))
            {
                var currentService = currentServiceData[companyInfo.Id].Data.List.ToList();
                var actualService = actualServiceData.Data.List.ToList();

                var actualBlockIds = new HashSet<string>(actualService.Select(s => s.Id));
                var currentBlockIds = new HashSet<string>(currentService.Select(s => s.Id));

                var modBlockCollection = currentService.Where(s => actualBlockIds.Contains(s.Id));

                var delBlockCollection = currentService.Where(s => !actualBlockIds.Contains(s.Id));
                var addBlockCollection = actualService.Where(s => !currentBlockIds.Contains(s.Id));

                if (delBlockCollection.Any() || addBlockCollection.Any())
                {
                    var message = $"[ {now} ] Обнаружены изменения в блоках услуг организации {companyInfo.Name}\n";

                    Console.Write(message);
                    content.AppendLine(message);
                }

                if (addBlockCollection.Any())
                {
                    content.AppendLine($"\t| Добавленные блоки:\n");

                    foreach (var block in addBlockCollection)
                    {
                        content.AppendLine($"\t\t| Блок: {block.Name}\n");
                        foreach (var service in block.Services)
                        {
                            content.AppendLine($"\t\t\t> {service.Price} {actualServiceData.Data.Currency.Abbr} ({service.Time} мин.) - {service.Name.Replace('\n', ' ')}");
                        }
                        content.AppendLine();
                    }
                }

                if (delBlockCollection.Any())
                {
                    content.AppendLine($"\t| Удаленные блоки:\n");

                    foreach (var block in delBlockCollection)
                    {
                        content.AppendLine($"\t\t| Блок: {block.Name}\n");
                        foreach (var service in block.Services)
                        {
                            content.AppendLine($"\t\t\t> {service.Price} {actualServiceData.Data.Currency.Abbr} ({service.Time} мин.) - {service.Name.Replace('\n', ' ')}");
                        }
                        content.AppendLine();
                    }
                }

                if (modBlockCollection.Any())
                {
                    foreach (var block in modBlockCollection)
                    {
                        var currentServiceIds = currentService.First(s => s.Id == block.Id).Services.Select(s => s.Id.ToString()).ToList();
                        var actualServiceIds = actualService.First(s => s.Id == block.Id).Services.Select(s => s.Id.ToString()).ToList();

                        var delServiceCollection = currentService.First(s => s.Id == block.Id).Services.Where(s => !actualServiceIds.Contains(s.Id.ToString())).ToList();
                        var addServiceCollection = actualService.First(s => s.Id == block.Id).Services.Where(s => !currentServiceIds.Contains(s.Id.ToString())).ToList();

                        var modServiceCollection = currentService.First(s => s.Id == block.Id).Services.Where(s => actualServiceIds.Contains(s.Id.ToString())).ToList();

                        if (delServiceCollection.Any() || addServiceCollection.Any())
                        {
                            var message = $"[ {now} ] Обнаружены изменения в блоке {block.Name} организации {companyInfo.Name}\n";

                            Console.Write(message);
                            content.AppendLine(message);
                        }

                        if (delServiceCollection.Any())
                        {
                            content.AppendLine($"\t| Удаленные услуги:\n");

                            foreach (var service in delServiceCollection)
                            {
                                content.AppendLine($"\t\t> {service.Price} {actualServiceData.Data.Currency.Abbr} ({service.Time} мин.) - {service.Name.Replace('\n', ' ')}");
                            }
                            content.AppendLine();
                        }

                        if (addServiceCollection.Any())
                        {
                            content.AppendLine($"\t| Добавленные услуги:\n");

                            foreach (var service in addServiceCollection)
                            {
                                content.AppendLine($"\t\t> {service.Price} {actualServiceData.Data.Currency.Abbr} ({service.Time} мин.) - {service.Name.Replace('\n', ' ')}");
                            }
                            content.AppendLine();
                        }

                        if (modServiceCollection.Any())
                        {
                            var messages = new List<string>();

                            foreach (var el in modServiceCollection)
                            {
                                var curService = currentService.First(s => s.Id == block.Id).Services.First(s => s.Id == el.Id);
                                var actlService = actualService.First(s => s.Id == block.Id).Services.First(s => s.Id == el.Id);

                                var isPrise = curService.Price != actlService.Price;
                                var isName = curService.Name != actlService.Name;
                                var isTime = curService.Time != actlService.Time;

                                var name = isName ? curService.Id.ToString() : curService.Name.Replace('\n', ' ');

                                var message = $"\t\t| Изменение услуги \"{name}\":\n\n";

                                if (isPrise || isName || isTime)
                                {
                                    if (isPrise) message += $"\t\t\t - Изменение стоимости: {curService.Price} -> {actlService.Price}\n";
                                    if (isName) message += $"\t\t\t - Изменение названия: {curService.Name} -> {actlService.Name}\n";
                                    if (isTime) message += $"\t\t\t - Изменение времени выполнения: {curService.Time} -> {actlService.Time}\n";

                                    messages.Add(message);
                                }
                            }

                            if (messages.Any()) 
                            {
                                content.AppendLine($"\t| Обнаружено изменение услуг в блоке {block.Name}\n");

                                foreach (var msg in messages) 
                                {
                                    content.AppendLine(msg);
                                }
                            }
                        }
                    }

                }
            }
            else
            {
                content.AppendLine($"[ {now} ] Актуальные услуги для организации {companyInfo.Name}\n");

                foreach (var block in actualServiceData.Data.List)
                {
                    content.AppendLine($"\t| Блок: {block.Name}\n");
                    foreach (var service in block.Services)
                    {
                        content.AppendLine($"\t\t> {service.Price} {actualServiceData.Data.Currency.Abbr} ({service.Time} мин.) - {service.Name.Replace('\n', ' ')}");
                    }
                    content.AppendLine();
                }
            }
        }
        catch (Exception ex)
        {
            content.AppendLine($"[ {now} ] Возникла ошибка при обновлении услуг: {ex.Message}\n");
        }
        finally
        {
            using (StreamWriter writer = new StreamWriter(serviceFile, append: true))
            {
                if (content.Length > 0)
                {
                    writer.Write(content);
                    writer.WriteLine("==================================================\n");
                }
            }

            currentServiceData[companyInfo.Id] = actualServiceData;
        }
    }
}