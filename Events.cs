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
  private void OnClientAuthorized(int playerSlot, SteamID steamId)
  {
    CCSPlayerController? player = Utilities.GetPlayerFromSlot(playerSlot);

    if (player == null || !player.IsValid || player.IsBot) return;
    ulong steamid = steamId.SteamId64;

    Task.Run(() => ReloadUserPermissions(steamid));
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
