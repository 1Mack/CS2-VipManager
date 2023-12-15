using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.Menu;

namespace VipManager;

public partial class VipManager
{
  public void StatusVip(CCSPlayerController? player, CommandInfo command)
  {

    if (player == null || !player.IsValid) return;

    if (!string.IsNullOrEmpty(Config.Commands.StatusPermission) && !AdminManager.PlayerHasPermissions(player, Config.Commands.StatusPermission.Split(";")))
    {
      command.ReplyToCommand($"{Localizer["Prefix"]} {Localizer["MissingCommandPermission"]}");
      return;
    }
    if (player == null || !player.IsValid) return;

    int playerIndex = (int)player.Index;

    if (commandCooldown != null && DateTime.UtcNow >= commandCooldown[playerIndex].AddSeconds(Config.CooldownRefreshCommandSeconds))
    {
      commandCooldown[playerIndex] = DateTime.UtcNow;

      var findPlayerAdmins = PlayerAdmins.FindAll(obj => obj.SteamId == player.SteamID.ToString());

      if (findPlayerAdmins == null)
      {
        command.ReplyToCommand($"{Localizer["Prefix"]} {Localizer["NoAdminsRole"]}");
        return;
      }
      var chatMenu = new ChatMenu(Localizer["RolesMenu"]);

      void handleMenu(CCSPlayerController player, ChatMenuOption option)
      {
        var getPlayer = findPlayerAdmins.Find(obj => obj.Group == option.Text.ToLower());
        if (getPlayer == null)
        {
          player.PrintToChat($"{Localizer["Prefix"]} {Localizer["RoleNotFound"]}");
          return;
        }
        player.PrintToChat(Localizer["Status", getPlayer.Group.ToUpper(), getPlayer.CreatedAt, getPlayer.EndAt]);
      }


      foreach (var item in findPlayerAdmins)
      {
        chatMenu.AddMenuOption(item.Group.ToUpper(), handleMenu);

      }
      ChatMenus.OpenMenu(player, chatMenu);
    }
    else
    {
      command.ReplyToCommand($"{Localizer["Prefix"]} {Localizer["CoolDown", Config.CooldownRefreshCommandSeconds]}");

    }

  }
}