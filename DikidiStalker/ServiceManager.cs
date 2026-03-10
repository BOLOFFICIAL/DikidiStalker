using System.Text;

namespace DikidiStalker
{
    public class ServiceManager
    {
        public static void Print(Dictionary<string, MasterInfo> masters, CompanyInfo companyInfo, ServiceDataResponse actualServiceData, ServiceUpdate serviceUpdate, string filePath)
        {
            var content = new StringBuilder();
            var now = DateTime.Now;

            if (serviceUpdate.Exception is null)
            {
                if (serviceUpdate.InitializeCollection is null)
                {
                    var hasChanges = false;
                    hasChanges |= serviceUpdate.DelBlockCollection.Any();
                    hasChanges |= serviceUpdate.AddBlockCollection.Any();
                    hasChanges |= serviceUpdate.AddServiceCollection.Any();
                    hasChanges |= serviceUpdate.DelServiceCollection.Any();
                    hasChanges |= serviceUpdate.ModServiceCollection.Any();

                    if (hasChanges)
                    {
                        var message = $"[ {now} ] Обнаружены изменения в услугах организации \"{companyInfo.Name}\"";

                        Console.WriteLine(message);
                        content.AppendLine($"{message}\n");
                    }

                    if (serviceUpdate.DelBlockCollection.Any()) 
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

                    if (serviceUpdate.AddBlockCollection.Any())
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

                    if (serviceUpdate.AddServiceCollection.Any())
                    {
                        content.AppendLine($"\t| Добавленные услуги:\n");

                        foreach (var block in serviceUpdate.AddServiceCollection)
                        {
                            content.AppendLine($"\t\t| Блок: {actualServiceData.Data.List.First(b=>b.Id == block.Key).Name}\n");
                            
                            foreach (var service in block.Value) 
                            {
                                content.AppendLine($"\t\t\t> {service.Price} {actualServiceData.Data.Currency.Abbr} ({service.Time} мин.) - {service.Name.Replace('\n', ' ')}");
                            }

                            content.AppendLine();
                        }
                    }

                    if (serviceUpdate.DelServiceCollection.Any())
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

                    if (serviceUpdate.ModServiceCollection.Any())
                    {
                        content.AppendLine($"\t| Измененные услуги:\n");

                        foreach (var block in serviceUpdate.ModServiceCollection)
                        {
                            content.AppendLine($"\t\t| Блок: {actualServiceData.Data.List.First(b => b.Id == block.Key).Name}\n");

                            foreach (var service in block.Value) 
                            {
                                content.AppendLine($"\t\t\t| Услуга: {actualServiceData.Data.List.First(b => b.Id == block.Key).Services.First(s => s.Id == service.Key).Name.Replace('\n',' ')}\n");

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
                else
                {
                    content.AppendLine($"[ {now} ] Актуальные услуги для организации \"{companyInfo.Name}\"\n");

                    var data = serviceUpdate.InitializeCollection;

                    foreach (var block in data.Data.List)
                    {
                        content.AppendLine($"\t| Блок: {block.Name}\n");

                        foreach (var service in block.Services)
                        {
                            content.AppendLine($"\t\t> {service.Price} {data.Data.Currency.Abbr} ({service.Time} мин.) - {service.Name.Replace('\n', ' ')}");
                        }
                        content.AppendLine();
                    }
                }
            }
            else
            {
                content.AppendLine($"[ {now} ] Возникла ошибка при анализе услуг: {serviceUpdate?.Exception}");
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
    }
}
