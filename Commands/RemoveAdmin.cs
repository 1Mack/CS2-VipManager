using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using Dapper;
using MySqlConnector;

namespace VipManager;

public partial class VipManager
{
  [CommandHelper(minArgs: 2, usage: "[steamid64] [group]", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
  public void RemoveAdmin(CCSPlayerController? player, CommandInfo command)
  {
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

    Task.Run(async () =>
    {


      try
      {
        using var connection = new MySqlConnection(DatabaseConnectionString);
        await connection.OpenAsync();

        string query = $"SELECT id FROM `{Config.Database.PrefixVipManager}` WHERE steamid = @steamid AND `group` = @group";

        IEnumerable<dynamic> result = await connection.QueryAsync(query, new { steamid = targetPlayer.Steamid, group = args[1] });


        if (result == null || result.AsList().Count == 0)
        {
          Server.NextFrame(() =>
          {
            command.ReplyToCommand($"{Localizer["Prefix"]} {Localizer["NoAdminWithSteamidAndGroup"]}");
          });
          return;
        }

        query = $"DELETE FROM `{Config.Database.PrefixVipManager}` WHERE steamid = @steamid AND `group` = @group";

        await connection.ExecuteAsync(query, new { steamid = targetPlayer.Steamid, group = args[1] });

        ReloadUserPermissions(targetPlayer.Steamid, args[1], "remove");

        Server.NextFrame(() =>
        {
          command.ReplyToCommand($"{Localizer["Prefix"]} {Localizer["AdminDeleteSuccess"]}");
        });

        await connection.CloseAsync();
      }
      catch (Exception e)
      {
        Console.WriteLine(e);
        Server.NextFrame(() =>
        {
          command.ReplyToCommand($"{Localizer["Prefix"]} {Localizer["InternalError"]}");
        });
        return;
      }
    });
  }
}