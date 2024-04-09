using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using Dapper;
using Microsoft.Extensions.Logging;
using MySqlConnector;

namespace VipManager;

public partial class VipManager
{
  [CommandHelper(minArgs: 3, usage: "[steamid64] [group (without #css/)] [time (minutes) or 0 (permanent)] [server_id (default is ServerID. 0 = all)]", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
  public void SetAdmin(CCSPlayerController? player, CommandInfo command)
  {
    if (string.IsNullOrEmpty(Config.Commands.AddPrefix))
    {
      command.ReplyToCommand($"{Localizer["Prefix"]} {Localizer["CommandDisabled"]}");

      return;
    }
    if (!string.IsNullOrEmpty(Config.Commands.AddPermission) && !AdminManager.PlayerHasPermissions(player, Config.Commands.AddPermission.Split(";")))
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

    int serverID = 0;

    if (args.Length == 4)
    {
      args[3] = args[3].Length != 0 ? args[3] : "0";
      serverID = int.TryParse(args[3], out int parsedValue) ? parsedValue : Config.ServerID;
    }
    else
      serverID = Config.ServerID;

    try
    {
      string query = "";
      Task<List<AdminsDatabaseClass>> task1 = Task.Run(async () =>
       {
         query = $"SELECT * FROM {Config.Database.PrefixVipManager} WHERE steamid = @steamid AND `group` = @group AND server_id = @serverID";

         return await QueryAsync<AdminsDatabaseClass>(query, new { steamid = targetPlayer.Steamid, group = args[1], serverID });
       });
      task1.Wait();

      if (task1.Result.Count > 0)
      {
        command.ReplyToCommand($"{Localizer["Prefix"]} {Localizer["AlreadyRegistryWithSteamidAndGroup"]}");
        return;
      }

      var endAt = args[2] == "0" ? 0 : DateTimeOffset.UtcNow.AddMinutes(int.Parse(args[2])).ToUnixTimeMilliseconds() / 1000;

      var createdAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000;

      Task task2 = Task.Run(() => SetRoleOnVipManagerDatabase(targetPlayer.Name ?? "Undefined", targetPlayer.Steamid, args[1], serverID, createdAt, endAt));

      task1.Wait();
      ReloadUserPermissions(targetPlayer.Steamid, args[1], "add", int.Parse(args[2]));
      command.ReplyToCommand($"{Localizer["Prefix"]} {Localizer["AdminAddSuccess"]}");
    }
    catch (Exception e)
    {
      Logger.LogError($"{Localizer["Prefix"]} {Localizer["InternalError"]} " + e.Message);

      command.ReplyToCommand($"{Localizer["Prefix"]} {Localizer["InternalError"]}");
    }
  }
}