using System.IO;
using System.Reflection;
using Newtonsoft.Json;

public class Config
{
    public string WebhookUrl = "https://discord.com/api/webhooks/TON_WEBHOOK_ICI";

    private static readonly string configPath;

    static Config()
    {
        // Récupère le chemin du répertoire de l'assembly
        string assemblyDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        string directoryPath = Path.Combine(assemblyDirectory, "NovaAdminsChat");

        // Définit le chemin complet du fichier JSON
        configPath = Path.Combine(directoryPath, "config.json");

        // Crée le dossier s'il n'existe pas
        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }
    }

    public static Config Load()
    {
        if (!File.Exists(configPath))
        {
            // Crée une configuration par défaut si le fichier n'existe pas
            var defaultConfig = new Config();
            File.WriteAllText(configPath, JsonConvert.SerializeObject(defaultConfig, Formatting.Indented));
            return defaultConfig;
        }

        // Charge la configuration existante
        return JsonConvert.DeserializeObject<Config>(File.ReadAllText(configPath));
    }
}
