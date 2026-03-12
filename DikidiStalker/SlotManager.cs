using DikidiStalker.Backup;
using DikidiStalker.Config;
using DikidiStalker.Models;
using System.Text;

namespace DikidiStalker
{
    public class SlotManager
    {
        private string _baseDirectory;
        private BackupManager _backupManager;

        public SlotManager(string baseDirectory)
        {
            _baseDirectory = baseDirectory;
            _backupManager = new BackupManager(_baseDirectory);
        }

        public Dictionary<string, Dictionary<DateTime, DataInfoResponse>> LoadBackup()
        {
            return _backupManager.LoadBackup<Dictionary<string, Dictionary<DateTime, DataInfoResponse>>>("SlotBackUp");
        }

        public void CheckDikidiSlots(Dictionary<string, Dictionary<DateTime, DataInfoResponse>> currentDataInfo, int dataInfoPeriod, DikidiCompany company)
        {
            var now = DateTime.Now.Date;

            var actualDataInfo = Enumerable.Range(0, dataInfoPeriod)
                .Select(day => now.AddDays(day))
                .Select(date => new
                {
                    Date = date,
                    Slots = DikidiInfo.GetAvailableTimeSlots(company, date)
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

            Print(masters, companyInfo, slotUpdate);

            currentDataInfo[company.CompanyId] = actualDataInfo;

            _backupManager.SaveBackup(currentDataInfo, "SlotBackUp");
        }

        private SlotUpdate UpdateCurrentSlots(Dictionary<DateTime, DataInfoResponse> currentDataInfo, Dictionary<DateTime, DataInfoResponse> actualDataInfo)
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

        private void Print(Dictionary<string, MasterInfo> masters, CompanyInfo companyInfo, SlotUpdate slotUpdate)
        {
            var filePath = CheckSlotFile(companyInfo.Id);

            var content = new StringBuilder();
            var now = DateTime.Now;

            if (slotUpdate.Exception is null)
            {
                if (slotUpdate.InitializeCollection.Count > 0)
                {
                    content.AppendLine($"[ {now} ]\tАктуальные слоты для организации \"{companyInfo.Name}\"\n");

                    foreach (var dataInfo in slotUpdate.InitializeCollection)
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
                else if (slotUpdate.AddCollection.Count != 0 || slotUpdate.DelCollection.Count != 0 || slotUpdate.NewCollection.Count != 0)
                {
                    var message = $"[ {now} ]\tОбнаружены изменения в слотах организации";

                    Console.WriteLine($"{message} ({companyInfo.Id})\t\"{companyInfo.Name}\"");
                    content.AppendLine($"{message} \"{companyInfo.Name}\"\n");

                    if (slotUpdate.AddCollection.Count != 0)
                    {
                        content.AppendLine($"\t| Добавленные слоты\n");

                        foreach (var slotEntry in slotUpdate.AddCollection)
                        {
                            content.AppendLine($"\t\t| Дата: {slotEntry.Key.ToShortDateString()}\n");

                            foreach (var masterEntry in slotEntry.Value)
                            {
                                content.AppendLine($"\t\t\t> {masters[masterEntry.Key].Username}\n");

                                foreach (var timeSlot in masterEntry.Value)
                                {
                                    content.AppendLine($"\t\t\t\t[ {DateTime.Parse(timeSlot).TimeOfDay} ]");
                                }

                                content.AppendLine();
                            }
                        }
                    }

                    if (slotUpdate.DelCollection.Count != 0)
                    {
                        content.AppendLine($"\t| Удаленные слоты\n");

                        foreach (var slotEntry in slotUpdate.DelCollection)
                        {
                            content.AppendLine($"\t\t| Дата: {slotEntry.Key.ToShortDateString()}\n");

                            foreach (var masterEntry in slotEntry.Value)
                            {
                                content.AppendLine($"\t\t\t> {masters[masterEntry.Key].Username}\n");

                                foreach (var timeSlot in masterEntry.Value)
                                {
                                    content.AppendLine($"\t\t\t\t[ {DateTime.Parse(timeSlot).TimeOfDay} ]");
                                }

                                content.AppendLine();
                            }
                        }
                    }

                    if (slotUpdate.NewCollection.Count != 0)
                    {
                        content.AppendLine($"\t| Новые слоты\n");

                        foreach (var mod in slotUpdate.NewCollection)
                        {
                            content.AppendLine($"\t\t| Дата: {mod.Key.ToShortDateString()}\n");

                            foreach (var time in mod.Value.Data.Times.Where(t => t.Key != "0"))
                            {
                                content.AppendLine($"\t\t\t> {mod.Value.Data?.Masters[time.Key].Username}\n");

                                foreach (var el in time.Value)
                                {
                                    content.AppendLine($"\t\t\t\t[ {DateTime.Parse(el).TimeOfDay} ]");
                                }

                                content.AppendLine();
                            }
                        }
                    }
                }
            }
            else
            {
                content.AppendLine($"[ {now} ]\tВозникла ошибка при анализе слотов организации \"{companyInfo.Name}\": {slotUpdate.Exception}");
            }

            using (StreamWriter writer = new StreamWriter(filePath, append: true))
            {
                if (content.Length > 0)
                {
                    writer.Write(content);
                    writer.WriteLine("==================================================\n");
                }
            }
        }

        private string CheckSlotFile(string? id)
        {
            if (!Directory.Exists(_baseDirectory)) Directory.CreateDirectory(_baseDirectory);

            var dikidiCompanyFolder = Path.Combine(_baseDirectory, $"Dikidi_{id}");

            if (!Directory.Exists(dikidiCompanyFolder))
            {
                Console.WriteLine($"[ {DateTime.Now} ]\tСоздание папки для организации {id}");
                Directory.CreateDirectory(dikidiCompanyFolder);
            }

            var infoFile = Path.Combine(dikidiCompanyFolder, "Info.dkdstlk");

            if (!File.Exists(infoFile))
                File.WriteAllText(infoFile, string.Empty);

            return infoFile;
        }
    }
}
