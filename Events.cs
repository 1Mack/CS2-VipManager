using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Entities;
using Microsoft.Extensions.Logging;

namespace VipManager;

public partial class VipManager
{
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
  public HookResult OnPlayerFullConnect(EventPlayerConnectFull @event, GameEventInfo info)
  {
    CCSPlayerController? player = @event.Userid;

    if (player == null || !player.IsValid || player.IsBot || player.AuthorizedSteamID == null) return HookResult.Continue;

    commandCooldown.TryAdd(player.Slot, DateTime.UtcNow);

    ulong steamid = player.AuthorizedSteamID.SteamId64;

    Task.Run(() => ReloadUserPermissions(steamid, true));

    return HookResult.Continue;
  }
  private void OnClientDisconnect(int playerSlot)
  {
    commandCooldown.Remove(playerSlot);
  }
}
