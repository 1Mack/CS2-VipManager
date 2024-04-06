using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Menu;
using Microsoft.Extensions.Logging;

namespace VipManager;

public partial class VipManager
{
  [CommandHelper(minArgs: 1, usage: "[steamid64]", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
  public void RemoveAdmin(CCSPlayerController? player, CommandInfo command)
  {

    if (player == null || !player.IsValid || player.IsBot) return;

    if (!string.IsNullOrEmpty(Config.Commands.RemovePermission) && !AdminManager.PlayerHasPermissions(player, Config.Commands.RemovePermission.Split(";")))
    {
      command.ReplyToCommand($"{Localizer["Prefix"]} {Localizer["MissingCommandPermission"]}");
      return;
    }

    if (player != null && !CanExecuteCommand(player.Slot))
    {
      command.ReplyToCommand($"{Localizer["Prefix"]} {Localizer["CoolDown", Config.CooldownRefreshCommandSeconds]}");
      return;
    }

    string[] args = command.ArgString.Split(" ");

    GetPlayerClass? targetPlayer = GetPlayer(args[0], command);

    if (targetPlayer == null) return;

    if (Config.Groups.Enabled && GroupsName.Find(g => g.ToLower() == args[1].ToLower()) == null)
    {
      command.ReplyToCommand($"{Localizer["Prefix"]} {Localizer["MissingGroup", args[1]]}");
      return;
    }

    args[1] = args[1].Replace("#css/", "").ToLower();

    string query = "";

    try
    {
      Task<List<AdminsDatabaseClass>> task1 = Task.Run(async () =>
       {
         query = $"SELECT * FROM {Config.Database.PrefixVipManager} WHERE steamid = @steamid AND `group` = @group";

         return await QueryAsync<AdminsDatabaseClass>(query, new { steamid = targetPlayer.Steamid, group = args[1] });
       });
      task1.Wait();

      if (task1.Result.Count > 0)
      {
        command.ReplyToCommand($"{Localizer["Prefix"]} {Localizer["NoAdminWithSteamidAndGroup"]}");
        return;
      }

      Menu(Localizer["Menu.SelectAdminsRemove"], player, handleMenu, task1.Result.Select(obj => $"{obj.server_id}+{obj.group}+").ToList());


      void handleMenu(CCSPlayerController player, ChatMenuOption option)
      {
        query = $"DELETE FROM `{Config.Database.PrefixVipManager}` WHERE steamid = @steamid AND `group` = @group AND server_id = @serverID";

        string[] infos = option.Text.Split("+");

        Task task2 = Task.Run(() => ExecuteAsync(query, new { steamid = targetPlayer.Steamid, group = infos[1], serverID = infos[0] }));

        task2.Wait();

        ReloadUserPermissions(targetPlayer.Steamid, args[1], "remove");

        command.ReplyToCommand($"{Localizer["Prefix"]} {Localizer["AdminDeleteSuccess"]}");

      }

    }
    catch (Exception e)
    {
      Logger.LogError($"{Localizer["Prefix"]} {Localizer["InternalError"]} " + e.Message);
      Server.NextFrame(() =>
      {
        command.ReplyToCommand($"{Localizer["Prefix"]} {Localizer["InternalError"]}");
      });
      return;
    }
  }
}