using System.IO;
using System.Reflection;
using Newtonsoft.Json;

public class Config
{
    public string WebhookUrl = "https://discord.com/api/webhooks/TON_WEBHOOK_ICI";
    public string AdminChatKey = "F11";

    private static readonly string configPath;

    static Config()
    {
        string assemblyDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        string directoryPath = Path.Combine(assemblyDirectory, "NovaAdminsChat");

        configPath = Path.Combine(directoryPath, "config.json");

        if (!Directory.Exists(directoryPath))
            Directory.CreateDirectory(directoryPath);
    }

    public static Config Load()
    {
        if (!File.Exists(configPath))
        {
            var defaultConfig = new Config();
            File.WriteAllText(configPath, JsonConvert.SerializeObject(defaultConfig, Formatting.Indented));
            return defaultConfig;
        }

        return JsonConvert.DeserializeObject<Config>(File.ReadAllText(configPath));
    }
}
