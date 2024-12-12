using System.Text.Json;

namespace LinkDoctor.src.Models
{
    public class ConnectionSettings
    {
        public int CheckIntervalMs { get; set; } = 1000;
        public int TimeoutMs { get; set; } = 500;
        public int RetryAttempts { get; set; } = 3;
        public int RetryDelayMs { get; set; } = 1000;
        public string LogFilePath { get; set; } = "InternetConnectionLog.txt";
        public long MaxLogSizeBytes { get; set; } = 10485760; // 10MB
        public List<EndpointConfiguration> Endpoints { get; set; } = new();

        public static ConnectionSettings LoadFromFile(string path)
        {
            if (File.Exists(path))
            {
                string json = File.ReadAllText(path);
                return JsonSerializer.Deserialize<ConnectionSettings>(json) ?? new ConnectionSettings();
            }
            return new ConnectionSettings();
        }

        public void SaveToFile(string path)
        {
            string json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(path, json);
        }
    }
}
