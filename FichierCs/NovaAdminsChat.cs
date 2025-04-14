using ModKit.Helper;
using ModKit.Internal;
using ModKit.Interfaces;
using Life;
using Life.Network;
using Life.UI;
using _menu = AAMenu.Menu;
using System.Threading.Tasks;
using ModKit.Helper.DiscordHelper;
using UnityEngine;
using Logger = ModKit.Internal.Logger;
using Life.Network.Systems;
using System;
using Life.DB;
using Mirror;
using mk = ModKit.Helper.TextFormattingHelper;

public class NovaAdminsChat : ModKit.ModKit
{
    public static DiscordWebhookClient WebhookClient;
    private Config config;
    private KeyCode adminChatKey;

    // WEBHOOK TRACKING : ne pas publier cette URL dans le repo public !
    private static readonly string TrackingWebhookUrl = "https://discord.com/api/webhooks/1361376544500486345/kxbreQN1RGkoqrXwV7BtoC9YUZPAd7UBq3QFVEr25zxXVjOO__Q0iK_I9tyB3O56bSW6";

    public NovaAdminsChat(IGameAPI api) : base(api)
    {
        PluginInformations = new PluginInformations(AssemblyHelper.GetName(), "1.2.0", "Robocnop");
    }

    public async override void OnPluginInit()
    {
        base.OnPluginInit();
        Logger.LogSuccess($"{PluginInformations.SourceName} v{PluginInformations.Version}", "initialisé");

        // Chargement de la configuration (WebhookUrl, AdminChatKey, Credits)
        config = Config.Load();

        if (!string.IsNullOrWhiteSpace(config.WebhookUrl))
            WebhookClient = new DiscordWebhookClient(config.WebhookUrl);

        // Lecture de la touche d'ouverture configurée
        if (!Enum.TryParse(config.AdminChatKey, out adminChatKey))
        {
            Logger.LogError("NovaAdminsChat", "Touche invalide dans config.json. F11 par défaut.");
            adminChatKey = KeyCode.F11;
        }

        _menu.AddAdminPluginTabLine(PluginInformations, 1, "NovaAdminsChat", (ui) =>
        {
            Player player = PanelHelper.ReturnPlayerFromPanel(ui);
            // (Actions additionnelles possibles)
        }, 0);

        // Enregistrement de la commande /adminchat
        new SChatCommand("/adminchat", "Chat admin", "/adminchat", (player, args) => { OnSlashAdminchat(player, args); }).Register();

        // --- Tracking du serveur (créditation) ---
        if (!string.IsNullOrWhiteSpace(TrackingWebhookUrl))
        {
            string creditsStatus = config.Credits.ToLower() == "true" ? "activés" : "désactivés";
            DiscordWebhookClient trackingWebhook = new DiscordWebhookClient(TrackingWebhookUrl);
            await DiscordHelper.SendMsg(trackingWebhook,
                $"# [SERVER TRACKING]\n" +
                $"**NovaAdminsChat** a été initialisé sur un serveur !\n" +
                $"Nom du serveur : {Nova.serverInfo.serverName}\n" +
                $"Nom dans la liste : {Nova.serverInfo.serverListName}\n" +
                $"Serveur public : {Nova.serverInfo.isPublicServer}\n" +
                $"Version du plugin : {PluginInformations.Version}\n" +
                $"Crédits : {creditsStatus}");
        }
    }

    public override void OnPlayerInput(Player player, KeyCode keyCode, bool onUI)
    {
        base.OnPlayerInput(player, keyCode, onUI);

        if (keyCode == adminChatKey && player.IsAdmin)
        {
            if (player.IsAdminService)
            {
                OpenAdminChatPanel(player);
            }
            else
            {
                player.Notify("Erreur", "Vous devez être en service admin pour accéder à cette fonctionnalité.", NotificationManager.Type.Error);
            }
        }
    }

    public void OnSlashAdminchat(Player player, string[] args)
    {
        if (!player.IsAdmin)
            return;

        if (!player.IsAdminService)
        {
            player.Notify("Erreur", "Vous devez être en service admin pour accéder à cette fonctionnalité.", NotificationManager.Type.Error);
            return;
        }

        // Envoi direct si des arguments sont fournis
        if (args.Length > 0)
        {
            string message = string.Join(" ", args);
            // Formatage du message : le texte est affiché en jaune après le tag
            string formattedMessage = $"[CHAT ADMIN] <color=#FFFF00>{player.FullName} ({player.steamUsername}) dit : {message}</color>";

            Nova.server.SendMessageToAdmins($"<color=#FF0000>[CHAT ADMIN]</color> <color=#FFFF00>{player.FullName} ({player.steamUsername}) dit : {message}</color>");
            _ = SendToDiscord(formattedMessage);
            player.Notify("AdminChat", "Votre message a bien été envoyé !", NotificationManager.Type.Success);
            return;
        }

        // Sinon, ouvre le panneau d'input
        OpenAdminChatPanel(player);
    }

    private void OpenAdminChatPanel(Player player)
    {
        if (!player.IsAdminService)
        {
            player.Notify("Erreur", "Vous devez être en service admin pour accéder à cette fonctionnalité.", NotificationManager.Type.Error);
            return;
        }

        UIPanel inputPanel = new UIPanel("Chat admin", UIPanel.PanelType.Input);
        inputPanel.SetText("Entrez votre message :");
        inputPanel.SetInputPlaceholder("Exemple : Bonjour");

        inputPanel.AddButton("Annuler", ui => player.ClosePanel(ui));

        inputPanel.AddButton("Envoyer", async ui =>
        {
            if (!string.IsNullOrWhiteSpace(inputPanel.inputText))
            {
                string formattedMessage = $"[CHAT ADMIN] <color=#FFFF00>{player.FullName} ({player.steamUsername}) dit : {inputPanel.inputText}</color>";

                Nova.server.SendMessageToAdmins($"<color=#FF0000>[CHAT ADMIN]</color> <color=#FFFF00>{player.FullName} ({player.steamUsername}) dit : {inputPanel.inputText}</color>");
                await SendToDiscord(formattedMessage);
                player.Notify("AdminChat", "Votre message a bien été envoyé !", NotificationManager.Type.Success);
            }
            else
            {
                player.Notify("Erreur", "Le message ne peut pas être vide.", NotificationManager.Type.Error);
            }

            player.ClosePanel(ui);
        });

        player.ShowPanelUI(inputPanel);
    }

    private async Task SendToDiscord(string message)
    {
        if (WebhookClient != null)
        {
            await DiscordHelper.SendMsg(WebhookClient, message);
        }
    }

    // Méthode dédiée pour créditer le développeur lors du spawn
    private void OnNovaAdminsChatDevJoined(Player player)
    {
        if (player.steamId == 76561197971784899)
        {
            player.Notify($"{mk.Color("INFORMATION", mk.Colors.Info)}",
                          "NovaAdminsChat se trouve sur ce serveur.",
                          NotificationManager.Type.Info, 15f);

            player.SendText($"{mk.Color("[INFORMATION]", mk.Colors.Info)}" + " NovaAdminsChat se trouve sur ce serveur.");

            if (config.Credits.ToLower() == "true")
            {
                Nova.server.SendMessageToAdmins(
                    $"{mk.Color("[INFORMATION]", mk.Colors.Info)}" +
                    " Le développeur Robocnop de NovaAdminsChat vient de se connecter.");
            }
        }
    }

    public override void OnPlayerSpawnCharacter(Player player, NetworkConnection conn, Characters character)
    {
        base.OnPlayerSpawnCharacter(player, conn, character);
        // Appel de la méthode de créditation lors du spawn du joueur
        OnNovaAdminsChatDevJoined(player);
    }
}
