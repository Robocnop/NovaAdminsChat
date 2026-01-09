using System;
using System.Threading.Tasks;
using Life;
using Life.DB;
using Life.Network;
using Life.UI;
using ModKit.Helper;
using ModKit.Helper.DiscordHelper;
using ModKit.Interfaces;
using UnityEngine;
using _menu = AAMenu.Menu;
using Logger = ModKit.Internal.Logger;
using mk = ModKit.Helper.TextFormattingHelper;

namespace NovaAdminsChat.FichierCs
{
    public class NovaAdminsChat : ModKit.ModKit
    {
        public static DiscordWebhookClient WebhookClient;
        private Config _config;
        private KeyCode _adminChatKey;

        public NovaAdminsChat(IGameAPI api) : base(api)
        {
            PluginInformations = new PluginInformations(
                AssemblyHelper.GetName(), 
                "1.3.0", 
                "Robocnop"
            );
        }

        public override void OnPluginInit()
        {
            base.OnPluginInit();

            try
            {
                // Chargement de la configuration
                _config = Config.Load();
                Logger.LogSuccess("[NovaAdminsChat] Configuration chargee", "NovaAdminsChat");

                // Initialisation du webhook Discord
                if (!string.IsNullOrWhiteSpace(_config.WebhookUrl) && 
                    _config.WebhookUrl.StartsWith("https://discord.com/api/webhooks/"))
                {
                    WebhookClient = new DiscordWebhookClient(_config.WebhookUrl);
                    Logger.LogSuccess("[NovaAdminsChat] Webhook Discord configure", "NovaAdminsChat");
                }
                else
                {
                    Logger.LogWarning("[NovaAdminsChat] Webhook Discord non configure", "NovaAdminsChat");
                }

                // Parse de la touche
                if (!Enum.TryParse(_config.AdminChatKey, out _adminChatKey))
                {
                    Logger.LogWarning($"[NovaAdminsChat] Touche '{_config.AdminChatKey}' invalide, F11 par defaut", "NovaAdminsChat");
                    _adminChatKey = KeyCode.F11;
                }

                // Menu admin
                _menu.AddAdminPluginTabLine(PluginInformations, 1, "NovaAdminsChat", (ui) =>
                {
                    Player player = PanelHelper.ReturnPlayerFromPanel(ui);
                    OpenAdminChatPanel(player);
                }, 0);

                // Commande
                new SChatCommand("/adminchat", "Envoyer un message admin", "/adminchat [message]", OnSlashAdminchat).Register();

                Logger.LogSuccess($"[NovaAdminsChat v{PluginInformations.Version}] Plugin initialise", "NovaAdminsChat");
            }
            catch (Exception ex)
            {
                Logger.LogError($"[NovaAdminsChat] Erreur initialisation: {ex.Message}", "NovaAdminsChat");
            }
        }

        public override void OnPlayerInput(Player player, KeyCode keyCode, bool onUI)
        {
            base.OnPlayerInput(player, keyCode, onUI);

            if (!player.IsAdmin) return;

            if (keyCode == _adminChatKey)
            {
                if (player.IsAdminService)
                {
                    OpenAdminChatPanel(player);
                }
                else
                {
                    player.Notify("Service Admin", "Vous devez etre en service admin.", NotificationManager.Type.Warning);
                }
            }
        }

        public void OnSlashAdminchat(Player player, string[] args)
        {
            if (!player.IsAdmin)
            {
                if (_config.VerboseLogs)
                    Logger.LogSuccess($"[NovaAdminsChat] Tentative acces non-admin: {player.steamUsername}", "NovaAdminsChat");
                return;
            }

            if (!player.IsAdminService)
            {
                player.Notify("Service Admin", "Vous devez etre en service admin.", NotificationManager.Type.Warning);
                return;
            }

            // Envoi direct si message fourni
            if (args.Length > 0)
            {
                string content = string.Join(" ", args);
                _ = SendAdminMessage(player, content);
                return;
            }

            // Sinon ouvre le panel
            OpenAdminChatPanel(player);
        }

        private void OpenAdminChatPanel(Player player)
        {
            if (!player.IsAdminService)
            {
                player.Notify("Service Admin", "Vous devez etre en service admin.", NotificationManager.Type.Warning);
                return;
            }

            UIPanel panel = new UIPanel("Chat Admin", UIPanel.PanelType.Input);
            panel.SetText("Entrez votre message a diffuser aux admins :");
            panel.SetInputPlaceholder("Votre message ici...");

            panel.AddButton("Annuler", ui => player.ClosePanel(ui));

            panel.AddButton("Envoyer", async ui =>
            {
                string content = panel.inputText?.Trim();
                
                if (string.IsNullOrWhiteSpace(content))
                {
                    player.Notify("Erreur", "Le message ne peut pas etre vide.", NotificationManager.Type.Error);
                    return;
                }

                await SendAdminMessage(player, content);
                player.ClosePanel(ui);
            });

            player.ShowPanelUI(panel);
        }

        private async Task SendAdminMessage(Player player, string content)
        {
            try
            {
                // Message en jeu (avec couleurs configurables)
                string steamInfo = _config.ShowSteamUsername ? $" ({player.steamUsername})" : "";
                string gameMessage = $"<color={_config.AdminChatColor}>[CHAT ADMIN]</color> " +
                                   $"<color={_config.MessageColor}>{player.FullName}{steamInfo}: {content}</color>";

                Nova.server.SendMessageToAdmins(gameMessage);

                // Message Discord
                await SendToDiscord(player, content);

                // Notification de succes
                player.Notify("AdminChat", "Message envoye !", NotificationManager.Type.Success);

                // Log
                if (_config.VerboseLogs)
                    Logger.LogSuccess($"[NovaAdminsChat] Message de {player.FullName}: {content}", "NovaAdminsChat");
            }
            catch (Exception ex)
            {
                Logger.LogError($"[NovaAdminsChat] Erreur envoi: {ex.Message}", "NovaAdminsChat");
                player.Notify("Erreur", "Erreur lors de l'envoi.", NotificationManager.Type.Error);
            }
        }

        private async Task SendToDiscord(Player player, string content)
        {
            if (WebhookClient == null) return;

            try
            {
                string steamInfo = _config.ShowSteamUsername ? $" ({player.steamUsername})" : "";
                string timestamp = $"`[{DateTime.Now:HH:mm:ss}]` ";
                
                string message = $"{timestamp}**[ADMIN CHAT]** {EscapeDiscord(player.FullName)}{steamInfo}: {EscapeDiscord(content)}";
                
                await DiscordHelper.SendMsg(WebhookClient, message);
                
                if (_config.VerboseLogs)
                    Logger.LogSuccess("[NovaAdminsChat] Message Discord envoye", "NovaAdminsChat");
            }
            catch (Exception ex)
            {
                Logger.LogError($"[NovaAdminsChat] Webhook Discord: {ex.Message}", "NovaAdminsChat");
            }
        }

        private string EscapeDiscord(string text)
        {
            return text.Replace("*", "\\*")
                       .Replace("_", "\\_")
                       .Replace("~", "\\~")
                       .Replace("`", "\\`")
                       .Replace("|", "\\|");
        }

        private void OnDevJoined(Player player)
        {
            if (player.steamId == 76561197971784899 && _config.Credits.ToLower() == "true")
            {
                player.Notify($"{mk.Color("NovaAdminsChat", mk.Colors.Info)}", 
                             "Ce serveur utilise NovaAdminsChat", 
                             NotificationManager.Type.Info);
            }
        }

        public override void OnPlayerSpawnCharacter(Player player, Mirror.NetworkConnection conn, Characters character)
        {
            base.OnPlayerSpawnCharacter(player, conn, character);
            OnDevJoined(player);
        }
    }
}
