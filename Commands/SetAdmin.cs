using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using Dapper;
using MySqlConnector;

namespace VipManager;

public partial class VipManager
{
  [CommandHelper(minArgs: 3, usage: "[steamid64 (without #css/)] [group] [time (minutes) or 0 (permanent)]", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
  public void SetAdmin(CCSPlayerController? player, CommandInfo command)
  {

    if (!string.IsNullOrEmpty(Config.Commands.AddPermission) && !AdminManager.PlayerHasPermissions(player, Config.Commands.AddPermission.Split(";")))
    {
      command.ReplyToCommand($"{Localizer["Prefix"]} {Localizer["MissingCommandPermission"]}");

      return;
    }
    string[] args = command.ArgString.Split(" ");

    GetPlayerClass? targetPlayer = GetPlayer(args[0], command);

    if (targetPlayer == null) return;

    try
    {
      Task.Run(async () =>
      {

        using var connection = new MySqlConnection(DatabaseConnectionString);
        await connection.OpenAsync();

        string query = "SELECT id FROM vip_manager WHERE steamid = @steamid AND `group` = @group";

        args[1] = args[1].Replace("#css/", "").ToLower();

        IEnumerable<dynamic> result = await connection.QueryAsync(query, new { steamid = targetPlayer.Steamid, group = args[1] });

        if (result != null && result.AsList().Count > 0)
        {
          Server.NextFrame(() =>
          {
            command.ReplyToCommand($"{Localizer["Prefix"]} {Localizer["AlreadyRegistryWithSteamidAndGroup"]}");
          });
          return;
        }
        var endAt = args[2] == "0" ? 0 : DateTimeOffset.UtcNow.AddMinutes(int.Parse(args[2])).ToUnixTimeMilliseconds() / 1000;
        var createdAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000;

        query = $"INSERT INTO `{Config.Database.PrefixVipManager}` (`name`, `steamid`, `group`, `created_at`,`end_at`) VALUES(@name, @steamid, @group, @createdAt, @endAt)";

        await connection.ExecuteAsync(query, new { name = targetPlayer.Name, steamid = targetPlayer.Steamid, group = args[1], createdAt, endAt });

        await connection.CloseAsync();



        Server.NextFrame(() =>
        {
          ReloadUserPermissions(targetPlayer.Steamid, args[1], "add", int.Parse(args[2]));
          command.ReplyToCommand($"{Localizer["Prefix"]} {Localizer["AdminAddSuccess"]}");
        });

      });
    }
    catch (Exception e)
    {
      Console.WriteLine(e);

      Server.NextFrame(() =>
      {
        command.ReplyToCommand($"{Localizer["Prefix"]} {Localizer["InternalError"]}");
      });
    }
  }
}