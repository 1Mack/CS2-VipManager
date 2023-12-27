using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Entities;
using Dapper;
using MySqlConnector;

namespace VipManager;

public partial class VipManager
{
  public void TesteVip(CCSPlayerController? player, CommandInfo command)
  {
    if (player == null || !player.IsValid) return;

    if (!string.IsNullOrEmpty(Config.Commands.TestPermission) && !AdminManager.PlayerHasPermissions(player, Config.Commands.TestPermission.Split(";")))
    {
      command.ReplyToCommand($"{Localizer["Prefix"]} {Localizer["MissingCommandPermission"]}");
      return;
    }

    if (CanExecuteCommand(player.Slot))
    {
      if (Config.VipTest.Time == 0 || Config.VipTest.Group.Length == 0)
      {
        command.ReplyToCommand($"{Localizer["Prefix"]} {Localizer["CommandBlocked"]}!");
        return;
      }

      if (Config.Groups.Enabled && GroupsName.Find(g => g.ToLower() == Config.VipTest.Group.ToLower()) == null)
      {
        command.ReplyToCommand($"{Localizer["Prefix"]} {Localizer["MissingGroup", Config.VipTest.Group]}");
        return;
      }

      string steamid = player.SteamID.ToString();
      string name = player.PlayerName;

      Task.Run(async () =>
      {
        using var connection = new MySqlConnection(DatabaseConnectionString);
        await connection.OpenAsync();

        string query = $"SELECT id FROM `{Config.Database.PrefixTestVip}` WHERE steamid = @steamid";


        IEnumerable<dynamic> result = await connection.QueryAsync(query, new { steamid });

        if (result != null && result.AsList().Count > 0)
        {
          Server.NextFrame(() =>
          {
            command.ReplyToCommand($"{Localizer["Prefix"]} {Localizer["TestVipAlreadyClaimed"]}");
          });
          return;
        }

        query = "SELECT id FROM vip_manager WHERE steamid = @steamid and `group` = @group";

        result = await connection.QueryAsync(query, new { steamid, group = Config.VipTest.Group });


        if (result != null && result.AsList().Count > 0)
        {
          Server.NextFrame(() =>
          {
            command.ReplyToCommand($"{Localizer["Prefix"]} {Localizer["AlreadyNormalVip"]}");
          });

          return;
        }

        var endAt = Config.VipTest.Time == 0 ? 0 : DateTimeOffset.UtcNow.AddMinutes(Config.VipTest.Time).ToUnixTimeMilliseconds() / 1000;
        var createdAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000;

        query = $"INSERT INTO `{Config.Database.PrefixTestVip}` (`name`, `steamid`, `created_at`,`end_at`) VALUES(@name, @steamid, @createdAt, @endAt)";

        await connection.ExecuteAsync(query, new { name, steamid, time = Config.VipTest.Time, createdAt, endAt });

        query = $"INSERT INTO `{Config.Database.PrefixVipManager}` (`name`, `steamid`, `group`, `created_at`, `end_at`) VALUES(@name, @steamid, @group, @createdAt, @endAt)";

        await connection.ExecuteAsync(query, new { name, steamid, group = Config.VipTest.Group, createdAt, endAt });


        Server.NextFrame(() =>
         {
           ReloadUserPermissions(steamid, Config.VipTest.Group, "add", Config.VipTest.Time);
           command.ReplyToCommand($"{Localizer["Prefix"]} {Localizer["TestVipActivated", Config.VipTest.Time]}");
         });

        return;
      });

    }


    command.ReplyToCommand($"{Localizer["Prefix"]} {Localizer["CoolDown", Config.CooldownRefreshCommandSeconds]}");

  }
}