using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Entities;

namespace VipManager;

public partial class VipManager
{
  public HookResult OnPlayerConnect(EventPlayerConnect @event, GameEventInfo info)
  {
    if (@event.Userid.IsValid && !@event.Bot && !@event.Userid.IsBot)
    {

      if (Config.ShowWelcomeMessageConnectedPrivate)
      {
        @event.Userid.PrintToChat(Localizer["WelComeMessage.ConnectPrivate", @event.Userid.PlayerName]);
      }
      if (Config.ShowWelcomeMessageConnectedPublic)
      {
        Server.PrintToChatAll(Localizer["WelComeMessage.ConnectPublic", @event.Userid.PlayerName]);
      }
    }

    return HookResult.Continue;
  }
  public HookResult OnPlayerDisconnect(EventPlayerDisconnect @event, GameEventInfo info)
  {
    if (!@event.Userid.IsBot)
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
    TestDatabaseConnection();
    GetAdminsFromDatabase();
  }
  private void OnClientAuthorized(int playerSlot, SteamID steamId)
  {
    CCSPlayerController? player = Utilities.GetPlayerFromSlot(playerSlot);

    if (player == null || !player.IsValid || player.IsBot) return;

    var findPlayerAdmins = PlayerAdmins.FindAll(obj => obj.SteamId == steamId.SteamId64.ToString());

    if (findPlayerAdmins != null)
    {
      AdminManager.AddPlayerToGroup(steamId, findPlayerAdmins.Select(obj => $"#css/{obj.Group}").ToArray());
    }

  }
}
