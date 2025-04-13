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

public class NovaAdminsChat : ModKit.ModKit
{
    public static DiscordWebhookClient WebhookClient;
    private Config config;
    private KeyCode adminChatKey;

    public NovaAdminsChat(IGameAPI api) : base(api)
    {
        PluginInformations = new PluginInformations(AssemblyHelper.GetName(), "1.1.0", "Robocnop");
    }

    public override void OnPluginInit()
    {
        base.OnPluginInit();
        Logger.LogSuccess($"{PluginInformations.SourceName} v{PluginInformations.Version}", "initialisé");

        config = Config.Load();

        if (!string.IsNullOrWhiteSpace(config.WebhookUrl))
            WebhookClient = new DiscordWebhookClient(config.WebhookUrl);

        // Lecture de la touche depuis la config
        if (!Enum.TryParse(config.AdminChatKey, out adminChatKey))
        {
            Logger.LogError("NovaAdminsChat", "Touche invalide dans config.json. F11 par défaut.");
            adminChatKey = KeyCode.F11;
        }

        _menu.AddAdminPluginTabLine(PluginInformations, 1, "NovaAdminsChat", (ui) =>
        {
            Player player = PanelHelper.ReturnPlayerFromPanel(ui);
        }, 0);

        new SChatCommand("/adminchat", "Chat admin", "/adminchat", (player, args) => { OnSlashAdminchat(player, args); }).Register();
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

        // Si des arguments sont fournis, envoyer directement
        if (args.Length > 0)
        {
            string message = string.Join(" ", args);
            string formattedMessage = $"[CHAT ADMIN] {player.FullName} ({player.steamUsername}) dit : {message}";

            Nova.server.SendMessageToAdmins($"<color=#FF0000>[CHAT ADMIN]</color> {player.FullName} ({player.steamUsername}) dit : {message}");
            _ = SendToDiscord(formattedMessage);
            player.Notify("AdminChat", "Votre message a bien été envoyé !", NotificationManager.Type.Success);
            return;
        }

        // Sinon, ouvrir le panel
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
                string formattedMessage = $"[CHAT ADMIN] {player.FullName} ({player.steamUsername}) dit : {inputPanel.inputText}";

                Nova.server.SendMessageToAdmins($"<color=#FF0000>[CHAT ADMIN]</color> {player.FullName} ({player.steamUsername}) dit : {inputPanel.inputText}");
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
}


// + le webhook perso
// + peux être les crédits