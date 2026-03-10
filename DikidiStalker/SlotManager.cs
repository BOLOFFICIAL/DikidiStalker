using System.Text;

namespace DikidiStalker
{
    public class SlotManager
    {
        public static void Print(Dictionary<string, MasterInfo> masters, CompanyInfo? companyInfo, SlotUpdate slotUpdate, string filePath)
        {
            var content = new StringBuilder();
            var now = DateTime.Now;

            if (slotUpdate.Exception is null)
            {
                if (slotUpdate.InitializeCollection.Count > 0)
                {
                    content.AppendLine($"[ {now} ] Актуальные слоты для организации \"{companyInfo.Name}\"\n");

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
                else if (slotUpdate.AddCollection.Any() || slotUpdate.DelCollection.Any() || slotUpdate.NewCollection.Any())
                {
                    var message = $"[ {now} ] Обнаружены изменения в слотах организации \"{companyInfo.Name}\"";
                    
                    Console.WriteLine(message);
                    content.AppendLine($"{message}\n");

                    if (slotUpdate.AddCollection.Any())
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

                    if (slotUpdate.DelCollection.Any())
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

                    if (slotUpdate.NewCollection.Any())
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
                content.AppendLine($"[ {now} ] Возникла ошибка при анализе слотов организации \"{companyInfo.Name}\": {slotUpdate.Exception}");
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
