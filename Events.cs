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
      if (!string.IsNullOrEmpty(Config.WelcomeMessage.WelcomePrivate))
      {
        @event.Userid.PrintToChat(Config.WelcomeMessage.WelcomePrivate);
      }
      if (!string.IsNullOrEmpty(Config.WelcomeMessage.WelcomePublic))
      {
        Server.PrintToChatAll(Config.WelcomeMessage.WelcomePublic);
      }
    }

    return HookResult.Continue;
  }
  public HookResult OnPlayerDisconnect(EventPlayerDisconnect @event, GameEventInfo info)
  {
    if (!@event.Userid.IsBot)
    {
      if (!string.IsNullOrEmpty(Config.WelcomeMessage.DisconnectPublic))
      {
        Server.PrintToChatAll(Config.WelcomeMessage.DisconnectPublic);
      }
    }
    return HookResult.Continue;
  }
  private void OnMapStart(string mapName)
  {
    TestDatabaseConnection();
    GetAdminsFromDatabase();
  }
  private void OnClientPutInServer(int playerSlot)
  {

    CCSPlayerController player = new(NativeAPI.GetEntityFromIndex((int)(uint)playerSlot + 1));
    var steamId = new SteamID(player.SteamID);

    var findPlayerAdmins = PlayerAdmins.FindAll(obj => obj.SteamId == steamId.SteamId64.ToString());

    if (findPlayerAdmins != null)
    {
      AdminManager.AddPlayerToGroup(steamId, findPlayerAdmins.Select(obj => $"#css/{obj.Group}").ToArray());
    }

  }
}
