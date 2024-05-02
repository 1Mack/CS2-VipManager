using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Entities;
using Microsoft.Extensions.Logging;

namespace VipManager;

public partial class VipManager
{
  public HookResult OnPlayerConnect(EventPlayerConnect @event, GameEventInfo info)
  {
    if (@event.Userid != null && @event.Userid.IsValid && !@event.Bot && !@event.Userid.IsBot)
    {
      if (Config.ShowWelcomeMessageConnectedPrivate)
      {
        @event.Userid.PrintToChat(Localizer["WelComeMessage.ConnectPrivate", @event.Userid.PlayerName]);
      }
      else if (Config.ShowWelcomeMessageConnectedPublic)
      {
        Server.PrintToChatAll(Localizer["WelComeMessage.ConnectPublic", @event.Userid.PlayerName]);
      }

    }

    return HookResult.Continue;
  }
  public HookResult OnPlayerDisconnect(EventPlayerDisconnect @event, GameEventInfo info)
  {
    if (!@event.Userid!.IsBot)
    {
      if (Config.ShowWelcomeMessageDisconnectedPublic)
      {
        Server.PrintToChatAll(Localizer["WelComeMessage.DisconnectedPublic", @event.Userid.PlayerName]);
      }
    }
    return HookResult.Continue;
  }
  private void OnMapStart(string mapName)
  {
    if (!isSynced)
      Task.Run(async () =>
      {
        await CreateDatabaseTables();
        if (Config.Groups.Enabled) await HandleGroupsFile();
        await GetAdminsFromDatabase();
      });
  }
  private void OnMapEnd()
  {
    isSynced = false;
  }
  private void OnClientAuthorized(int playerSlot, SteamID steamId)
  {
    CCSPlayerController? player = Utilities.GetPlayerFromSlot(playerSlot);

    if (player == null || !player.IsValid || player.IsBot) return;

    if (!isSynced)
    {
      Task.Run(async () =>
      {
        int cont = 0;
        while (!isSynced && cont < 6)
        {
          cont += 1;
          await Task.Delay(2000);
          Logger.LogError("The Admin List has not been synchronized yet, trying to sync again...");
        }

        if (cont >= 6)
        {
          Logger.LogError("The Admin List has not been synchronized. The plugin has stopped trying to sync, check for the issue.");
          return;
        }
      });
    }

    var findPlayerAdmins = PlayerAdmins.FindAll(obj => obj.SteamId == steamId.SteamId64.ToString());

    if (findPlayerAdmins != null)
    {
      AdminManager.AddPlayerToGroup(steamId, findPlayerAdmins.Select(obj => $"#css/{obj.Group}").ToArray());
    }
  }
  private void OnClientDisconnect(int playerSlot)
  {
    commandCooldown.Remove(playerSlot);
  }
  private void OnClientPutInServer(int playerSlot)
  {
    commandCooldown.Add(playerSlot, DateTime.UtcNow);
  }
}
