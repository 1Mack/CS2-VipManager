using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Menu;

namespace VipManager;

public partial class VipManager
{
  public void StatusVip(CCSPlayerController? player, CommandInfo command)
  {
    if (string.IsNullOrEmpty(Config.Commands.StatusPrefix))
    {
      command.ReplyToCommand($"{Localizer["Prefix"]} {Localizer["CommandDisabled"]}");

      return;
    }

    if (player == null || !player.IsValid) return;
    if (!string.IsNullOrEmpty(Config.Commands.StatusPermission) && !AdminManager.PlayerHasPermissions(player, Config.Commands.StatusPermission.Split(";")))
    {
      command.ReplyToCommand($"{Localizer["Prefix"]} {Localizer["MissingCommandPermission"]}");
      return;
    }
    if (CanExecuteCommand(player.Slot))
    {
      var findPlayerAdmins = PlayerAdmins.FindAll(obj => obj.SteamId == player.SteamID.ToString());

      if (findPlayerAdmins == null)
      {
        command.ReplyToCommand($"{Localizer["Prefix"]} {Localizer["NoAdminsRole"]}");
        return;
      }

      void handleMenu(CCSPlayerController player, ChatMenuOption option)
      {
        var getPlayer = findPlayerAdmins.Find(obj => obj.Group.ToLower() == option.Text.ToLower());
        if (getPlayer == null)
        {
          player.PrintToChat($"{Localizer["Prefix"]} {Localizer["RoleNotFound"]}");
          return;
        }
        player.PrintToChat(Localizer["Status", getPlayer.Group.ToUpper(), getPlayer.CreatedAt, getPlayer.EndAt]);
      }

      Menu(Localizer["RolesMenu"], player, handleMenu, findPlayerAdmins.Select(obj => obj.Group.ToUpper()).ToList(), Config.CloseMenuAfterUse);

    }
    else
    {
      command.ReplyToCommand($"{Localizer["Prefix"]} {Localizer["CoolDown", Config.CooldownRefreshCommandSeconds]}");

    }

  }
}