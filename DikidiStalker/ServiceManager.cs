using DikidiStalker.Backup;
using DikidiStalker.Config;
using DikidiStalker.Models;
using System.Text;

namespace DikidiStalker
{
    public class ServiceManager
    {
        private string _baseDirectory;
        private BackupManager _backupManager;

        public ServiceManager(string baseDirectory)
        {
            _baseDirectory = baseDirectory;
            _backupManager = new BackupManager(_baseDirectory);
        }

        public Dictionary<string, Dictionary<string, (MasterInfo, ServiceDataResponse)>> LoadBackup()
        {
            return _backupManager.LoadBackup<Dictionary<string, Dictionary<string, (MasterInfo, ServiceDataResponse)>>>("ServiceBackUp");
        }

        public void CheckDikidiService(Dictionary<string, Dictionary<DateTime, DataInfoResponse>> currentDataInfo, Dictionary<string, Dictionary<string, (MasterInfo, ServiceDataResponse)>> currentServiceData, DikidiCompany company)
        {
            var companyInfo = currentDataInfo.FirstOrDefault(i => i.Key == company.CompanyId.ToString()).Value?.FirstOrDefault().Value?.Data?.Company;

            var masters = currentDataInfo
                .Where(c=>c.Key == company.CompanyId.ToString())
                .SelectMany(outerKvp => outerKvp.Value)
                .SelectMany(innerKvp => innerKvp.Value.Data.Masters)
                .GroupBy(m => m.Key)
                .ToDictionary(g => g.Key, g => g.Last().Value);

            var actualServiceData = new Dictionary<string, (MasterInfo, ServiceDataResponse)>();
            var serviceUpdate = new Dictionary<string, (MasterInfo, ServiceUpdate)>();

            if (companyInfo is null) return;

            foreach (var master in masters.Where(m => m.Value.Id != "0"))
            {
                var actualServiceDataForMaster = DikidiInfo.GetCompanyServices(company, master.Key);

                if (actualServiceDataForMaster is null) return;

                var serviceUpdateForMaster = new ServiceUpdate();

                try
                {
                    if (currentServiceData.TryGetValue(company.CompanyId, out var current) && current.TryGetValue(master.Key, out var currentMaster))
                    {
                        serviceUpdateForMaster = UpdateCurrentService(companyInfo, currentMaster.Item2, actualServiceDataForMaster);
                    }
                    else
                    {
                        serviceUpdateForMaster.InitializeCollection = actualServiceDataForMaster;
                    }
                }
                catch (Exception ex)
                {
                    serviceUpdateForMaster.Exception = ex.Message;
                }

                actualServiceData.Add(master.Key, (master.Value, actualServiceDataForMaster));
                serviceUpdate.Add(master.Key, (master.Value, serviceUpdateForMaster));
            }

            PrintActual(companyInfo, actualServiceData);

            PrintUpdate(companyInfo, actualServiceData, serviceUpdate);

            currentServiceData[company.CompanyId] = actualServiceData;

            _backupManager.SaveBackup(currentServiceData, "ServiceBackUp");
        }

        private ServiceUpdate UpdateCurrentService(CompanyInfo companyInfo, ServiceDataResponse currentServiceData, ServiceDataResponse actualServiceData)
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

        private void PrintUpdate(CompanyInfo companyInfo, Dictionary<string, (MasterInfo, ServiceDataResponse)> actualServiceDataAll, Dictionary<string, (MasterInfo, ServiceUpdate)> serviceUpdateAll)
        {
            var content = new StringBuilder();
            var now = DateTime.Now;

            foreach (var el in serviceUpdateAll)
            {
                var serviceUpdate = el.Value.Item2;
                var actualServiceData = actualServiceDataAll[el.Key].Item2;

                if (serviceUpdate.Exception is null)
                {
                    if (serviceUpdate.InitializeCollection is null)
                    {
                        var hasChanges = false;
                        hasChanges |= serviceUpdate.DelBlockCollection.Count != 0;
                        hasChanges |= serviceUpdate.AddBlockCollection.Count != 0;
                        hasChanges |= serviceUpdate.AddServiceCollection.Count != 0;
                        hasChanges |= serviceUpdate.DelServiceCollection.Count != 0;
                        hasChanges |= serviceUpdate.ModServiceCollection.Count != 0;

                        if (hasChanges)
                        {
                            var message = $"[ {now} ]\tОбнаружены изменения в услугах мастера {el.Value.Item1.Username} организации";

                            Writer.ConsoleWriteLine($"{message} ({companyInfo.Id})\t\"{companyInfo.Name}\"", ConsoleColor.DarkYellow);
                            content.AppendLine($"{message} \"{companyInfo.Name}\"\n");
                        }

                        if (serviceUpdate.DelBlockCollection.Count != 0)
                        {
                            content.AppendLine($"\t| Удаленные блоки:\n");

                            foreach (var block in serviceUpdate.DelBlockCollection)
                            {
                                content.AppendLine($"\t\t| Блок {block.Name}\n");

                                foreach (var service in block.Services)
                                {
                                    content.AppendLine($"\t\t\t> {service.Price} {actualServiceData.Data.Currency.Abbr} ({service.Time} мин.) - {service.Name.Replace('\n', ' ')}");
                                }

                                content.AppendLine();
                            }
                        }

                        if (serviceUpdate.AddBlockCollection.Count != 0)
                        {
                            content.AppendLine($"\t| Добавленные блоки:\n");

                            foreach (var block in serviceUpdate.AddBlockCollection)
                            {
                                content.AppendLine($"\t\t| Блок {block.Name}\n");

                                foreach (var service in block.Services)
                                {
                                    content.AppendLine($"\t\t\t> {service.Price} {actualServiceData.Data.Currency.Abbr} ({service.Time} мин.) - {service.Name.Replace('\n', ' ')}");
                                }

                                content.AppendLine();
                            }
                        }

                        if (serviceUpdate.AddServiceCollection.Count != 0)
                        {
                            content.AppendLine($"\t| Добавленные услуги:\n");

                            foreach (var block in serviceUpdate.AddServiceCollection)
                            {
                                content.AppendLine($"\t\t| Блок: {actualServiceData.Data.List.First(b => b.Id == block.Key).Name}\n");

                                foreach (var service in block.Value)
                                {
                                    content.AppendLine($"\t\t\t> {service.Price} {actualServiceData.Data.Currency.Abbr} ({service.Time} мин.) - {service.Name.Replace('\n', ' ')}");
                                }

                                content.AppendLine();
                            }
                        }

                        if (serviceUpdate.DelServiceCollection.Count != 0)
                        {
                            content.AppendLine($"\t| Удаленные услуги:\n");

                            foreach (var block in serviceUpdate.DelServiceCollection)
                            {
                                content.AppendLine($"\t\t| Блок: {actualServiceData.Data.List.First(b => b.Id == block.Key).Name}\n");

                                foreach (var service in block.Value)
                                {
                                    content.AppendLine($"\t\t\t> {service.Price} {actualServiceData.Data.Currency.Abbr} ({service.Time} мин.) - {service.Name.Replace('\n', ' ')}");
                                }

                                content.AppendLine();
                            }
                        }

                        if (serviceUpdate.ModServiceCollection.Count != 0)
                        {
                            content.AppendLine($"\t| Измененные услуги:\n");

                            foreach (var block in serviceUpdate.ModServiceCollection)
                            {
                                content.AppendLine($"\t\t| Блок: {actualServiceData.Data.List.First(b => b.Id == block.Key).Name}\n");

                                foreach (var service in block.Value)
                                {
                                    content.AppendLine($"\t\t\t| Услуга: {actualServiceData.Data.List.First(b => b.Id == block.Key).Services.First(s => s.Id == service.Key).Name.Replace('\n', ' ')}\n");

                                    var value = service.Value;

                                    if (value.Item1.Price != value.Item2.Price)
                                    {
                                        content.AppendLine($"\t\t\t\t| Цена: {value.Item1.Price} -> {value.Item2.Price}");
                                    }

                                    if (value.Item1.Name != value.Item2.Name)
                                    {
                                        content.AppendLine($"\t\t\t\t| Название: {value.Item1.Name.Replace('\n', ' ')} -> {value.Item2.Name.Replace('\n', ' ')}");
                                    }

                                    if (value.Item1.Time != value.Item2.Time)
                                    {
                                        content.AppendLine($"\t\t\t\t| Время: {value.Item1.Time} -> {value.Item2.Time}");
                                    }

                                    content.AppendLine();
                                }
                            }
                        }
                    }
                }
                else
                {
                    var message = $"[ {now} ]\tВозникла ошибка при анализе услуг мастера {el.Value.Item1.Username} организации \"{companyInfo.Name}\": {serviceUpdate.Exception}";
                    Writer.ConsoleWriteLine(message, ConsoleColor.Red);
                    content.AppendLine(message);
                }
            }

            var serviceFiles = GetServiceFilePaths(companyInfo.Id);

            if (content.Length > 0)
            {
                Writer.WriteFile(content.Append("==================================================\n\n").ToString(), serviceFiles.update, append: true);
            }
        }

        private void PrintActual(CompanyInfo companyInfo, Dictionary<string, (MasterInfo, ServiceDataResponse)> actualcollectionAll)
        {
            var content = new StringBuilder();
            var now = DateTime.Now;

            content.AppendLine($"[ {now} ]\tАктуальные услуги для организации \"{companyInfo.Name}\"\n");

            foreach (var el in actualcollectionAll) 
            {
                var actualcollection = el.Value.Item2;

                content.AppendLine($"\t| Мастер: {el.Value.Item1.Username}\n");

                foreach (var block in actualcollection.Data.List)
                {
                    content.AppendLine($"\t\t| Блок: {block.Name}\n");

                    foreach (var service in block.Services)
                    {
                        content.AppendLine($"\t\t\t> {service.Price} {actualcollection.Data.Currency.Abbr} ({service.Time} мин.) - {service.Name.Replace('\n', ' ')}");
                    }
                    content.AppendLine();
                }
            }

            var serviceFiles = GetServiceFilePaths(companyInfo.Id);

            if (content.Length > 0)
            {
                Writer.WriteFile(content.ToString(), serviceFiles.actual);
            }
        }

        private (string actual, string update) GetServiceFilePaths(string? id)
        {
            if (!Directory.Exists(_baseDirectory)) Directory.CreateDirectory(_baseDirectory);

            var dikidiCompanyFolder = Path.Combine(_baseDirectory, $"Dikidi_{id}");

            if (!Directory.Exists(dikidiCompanyFolder))
            {
                Writer.ConsoleWriteLine($"[ {DateTime.Now} ]\tСоздание папки для организации {id}", ConsoleColor.Blue);
                Directory.CreateDirectory(dikidiCompanyFolder);
            }

            dikidiCompanyFolder = Path.Combine(dikidiCompanyFolder, $"ServicesInfo");

            if (!Directory.Exists(dikidiCompanyFolder))
            {
                Directory.CreateDirectory(dikidiCompanyFolder);
            }

            var actualInfoFile = Path.Combine(dikidiCompanyFolder, "ActualService.dkdstlk");

            if (!File.Exists(actualInfoFile))
                File.WriteAllText(actualInfoFile, string.Empty);

            var updateInfoFile = Path.Combine(dikidiCompanyFolder, "UpdateService.dkdstlk");

            if (!File.Exists(updateInfoFile))
                File.WriteAllText(updateInfoFile, string.Empty);

            return (actualInfoFile, updateInfoFile);
        }
    }
}
