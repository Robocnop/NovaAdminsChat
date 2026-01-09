using System.IO;
using System.Reflection;
using Newtonsoft.Json;

namespace NovaAdminsChat.FichierCs
{
    public class Config
    {
        public string WebhookUrl { get; set; } = "https://discord.com/api/webhooks/TON_WEBHOOK_ICI";
        public string AdminChatKey { get; set; } = "F11";
        public bool ShowSteamUsername { get; set; } = true;
        public string AdminChatColor { get; set; } = "#FF0000";
        public string MessageColor { get; set; } = "#FFFF00";
        public bool VerboseLogs { get; set; } = false;
        public string Credits { get; set; } = "true";

        private static string _configPath;

        public static void Initialize()
        {
            string assemblyDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string directoryPath = Path.Combine(assemblyDirectory, "NovaAdminsChat");
            _configPath = Path.Combine(directoryPath, "config.json");

            if (!Directory.Exists(directoryPath))
                Directory.CreateDirectory(directoryPath);
        }

        public static Config Load()
        {
            Initialize();

            if (!File.Exists(_configPath))
            {
                var defaultConfig = new Config();
                Save(defaultConfig);
                CreateReadme();
                return defaultConfig;
            }

            try
            {
                return JsonConvert.DeserializeObject<Config>(File.ReadAllText(_configPath));
            }
            catch
            {
                var defaultConfig = new Config();
                Save(defaultConfig);
                return defaultConfig;
            }
        }

        public static void Save(Config config)
        {
            File.WriteAllText(_configPath, JsonConvert.SerializeObject(config, Formatting.Indented));
        }

        private static void CreateReadme()
        {
            string readmePath = Path.Combine(Path.GetDirectoryName(_configPath), "CONFIG_README.txt");
            
            string readmeContent = @"=====================================================
  NOVAADMINSCHAT - CONFIGURATION
=====================================================

Ce fichier explique chaque option du fichier config.json

-----------------------------------------------------
WebhookUrl (texte)
-----------------------------------------------------
URL du webhook Discord pour recevoir les messages.
Exemple: https://discord.com/api/webhooks/123456/abcdef

Comment creer un webhook Discord:
1. Parametres du salon > Integrations > Webhooks
2. Nouveau Webhook
3. Copier l'URL du webhook

-----------------------------------------------------
AdminChatKey (texte)
-----------------------------------------------------
Touche pour ouvrir le chat admin.
Exemples: F11, F10, Insert, Delete, KeypadEnter

Liste complete des touches:
https://docs.unity3d.com/ScriptReference/KeyCode.html

-----------------------------------------------------
ShowSteamUsername (true/false)
-----------------------------------------------------
Afficher le pseudo Steam dans les messages.
- true: affiche ""John Doe (JohnSteam)""
- false: affiche ""John Doe""

-----------------------------------------------------
AdminChatColor (couleur hex)
-----------------------------------------------------
Couleur du tag [CHAT ADMIN] en hexadecimal.
Defaut: #FF0000 (rouge)
Exemples:
  - Rouge: #FF0000
  - Bleu: #0000FF
  - Vert: #00FF00
  - Orange: #FFA500

-----------------------------------------------------
MessageColor (couleur hex)
-----------------------------------------------------
Couleur du message en hexadecimal.
Defaut: #FFFF00 (jaune)

-----------------------------------------------------
VerboseLogs (true/false)
-----------------------------------------------------
Activer les logs detailles pour le debug.
- true: affiche tous les messages dans la console
- false: affiche uniquement les erreurs et infos importantes

Utile pour debugger les problemes.

-----------------------------------------------------
Credits (""true""/""false"")
-----------------------------------------------------
Afficher une notification au developpeur s'il se connecte.
- ""true"": affiche une notification discrete
- ""false"": aucune notification

=====================================================
";
            
            File.WriteAllText(readmePath, readmeContent);
        }
    }
}

