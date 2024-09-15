using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;

namespace VipManager;

public partial class VipManager
{
  public void ReloadAdmins(CCSPlayerController? player, CommandInfo command)
  {

    if (string.IsNullOrEmpty(Config.Commands.ReloadPrefix))
    {
      command.ReplyToCommand($"{Localizer["Prefix"]} {Localizer["CommandDisabled"]}");

      return;
    }

    if (!string.IsNullOrEmpty(Config.Commands.ReloadPermission) && !AdminManager.PlayerHasPermissions(player, Config.Commands.ReloadPermission.Split(";")))
    {
      command.ReplyToCommand($"{Localizer["Prefix"]} {Localizer["MissingCommandPermission"]}");
      return;
    }
    if (player != null && !CanExecuteCommand(player.Slot))
    {
      command.ReplyToCommand($"{Localizer["Prefix"]} {Localizer["CoolDown", Config.CooldownRefreshCommandSeconds]}");
      return;

    }
    ulong steamid;
    foreach (var p in Utilities.GetPlayers().Where(p => p != null && p.IsValid && !p.IsBot && p.AuthorizedSteamID != null))
    {
      steamid = p.AuthorizedSteamID!.SteamId64;
      Task.Run(() => ReloadUserPermissions(steamid));

    }

    command.ReplyToCommand($"{Localizer["Prefix"]} {Localizer["AdminReloadSuccess"]}");
  }
}