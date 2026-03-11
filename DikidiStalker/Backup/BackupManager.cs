using Newtonsoft.Json;

namespace DikidiStalker.Backup
{
    internal class BackupManager
    {
        private string _backUpDirectory;

        public BackupManager(string baseDirectory)
        {
            _backUpDirectory = Path.Combine(baseDirectory, "_backUp");

            if (!Directory.Exists(_backUpDirectory))
            {
                Directory.CreateDirectory(_backUpDirectory);
                File.SetAttributes(_backUpDirectory, FileAttributes.Hidden);
            }
        }

        public T LoadBackup<T>(string fileName)
        {
            try
            {
                var backUpfile = Path.Combine(_backUpDirectory, $"{fileName}.dkdbackup");
                if (!File.Exists(backUpfile)) return default;
                var backUp = File.ReadAllText(backUpfile);
                return JsonConvert.DeserializeObject<T>(backUp);
            }
            catch
            {
                return default;
            }
        }

        public void SaveBackup<T>(T currentData, string fileName)
        {
            try
            {
                var backUpfile = Path.Combine(_backUpDirectory, $"{fileName}.dkdbackup");
                using (StreamWriter writer = new StreamWriter(backUpfile))
                {
                    writer.Write(JsonConvert.SerializeObject(currentData));
                }
            }
            catch { }
        }
    }
}
