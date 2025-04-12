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

public class NovaAdminsChat : ModKit.ModKit
{
    public static DiscordWebhookClient WebhookClient;
    private Config config;

    public NovaAdminsChat(IGameAPI api) : base(api)
    {
        PluginInformations = new PluginInformations(AssemblyHelper.GetName(), "1.0.1", "Robocnop");
    }

    public override void OnPluginInit()
    {
        base.OnPluginInit();
        Logger.LogSuccess($"{PluginInformations.SourceName} v{PluginInformations.Version}", "initialisé");

        config = Config.Load();

        if (!string.IsNullOrWhiteSpace(config.WebhookUrl))
        {
            WebhookClient = new DiscordWebhookClient(config.WebhookUrl);
        }

        _menu.AddAdminPluginTabLine(PluginInformations, 1, "NovaAdminsChat", (ui) =>
        {
            Player player = PanelHelper.ReturnPlayerFromPanel(ui);

            // Ceci est totalement useless
            if (player.IsAdminService)
            {
                player.Notify("NovaAdminsChat", "Tu as cliqué sur NovaAdminsChat !", NotificationManager.Type.Info, 5);
            }
            else
            {
                player.Notify("Erreur", "Vous n'avez pas les permissions pour accéder à cette fonctionnalité.", NotificationManager.Type.Error);
            }
        }, 0);

        new SChatCommand("/achat", "Chat admin", "/achat", (player, args) => { OnSlashAchat(player); }).Register();
    }

    public override void OnPlayerInput(Player player, KeyCode keyCode, bool onUI)
    {
        base.OnPlayerInput(player, keyCode, onUI);

        // Vérifie si le joueur est admin avant d'ouvrir le panneau
        if (keyCode == KeyCode.F6 && player.IsAdminService)
        {
            OpenAdminChatPanel(player);
        }
        else if (keyCode == KeyCode.F6)
        {
            player.Notify("Erreur", "Vous devez être en service admin pour accéder à cette fonctionnalité.", NotificationManager.Type.Error);
        }
    }

    public void OnSlashAchat(Player player)
    {
        // Vérifie si le joueur est en service admin avant d'ouvrir le panneau
        if (player.IsAdminService)
        {
            OpenAdminChatPanel(player);
        }
        else
        {
            player.Notify("Erreur", "Vous devez être en service admin pour accéder à cette fonctionnalité.", NotificationManager.Type.Error);
        }
    }

    private void OpenAdminChatPanel(Player player)
    {
        // Vérifie si le joueur est en service admin avant d'ouvrir le panneau.
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
                string formattedMessage = $"[CHAT ADMIN] {player.FullName} ({player.steamUsername}) : {inputPanel.inputText}";

                // Message in-game
                Nova.server.SendMessageToAdmins($"<color=#FF0000>[CHAT ADMIN]</color> {player.FullName} ({player.steamUsername}): {inputPanel.inputText}");

                // Message Discord
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

// rajouter pour plus tard le fait que si on est pas admin on ne peux pas intéragire avec la cmd et le keybind. Et pas laisser vous devez être en service admin.
// + le webhook perso
// + peux être les crédits