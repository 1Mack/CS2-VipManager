using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using Microsoft.Extensions.Logging;

namespace VipManager;

public partial class VipManager
{
  public void TesteVip(CCSPlayerController? player, CommandInfo command)
  {

    if (string.IsNullOrEmpty(Config.Commands.TestPrefix))
    {
      command.ReplyToCommand($"{Localizer["Prefix"]} {Localizer["CommandDisabled"]}");

      return;
    }

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

      if (Config.Groups.Enabled && GroupsName.Find(g => g.Equals(Config.VipTest.Group, StringComparison.CurrentCultureIgnoreCase)) == null)
      {
        command.ReplyToCommand($"{Localizer["Prefix"]} {Localizer["MissingGroup", Config.VipTest.Group]}");
        return;
      }

      string steamid = player.SteamID.ToString();
      string name = player.PlayerName;

      try
      {
        string query = "";

        int serverID = Config.VipTest.FollowServerID ? Config.ServerID : 0;

        Task<List<object>> task1 = Task.Run(() =>
         {
           query = $"SELECT id FROM `{Config.Database.PrefixTestVip}` WHERE steamid = @steamid AND server_id = @serverID";
           return QueryAsync<object>(query, new { steamid, serverID });
         });
        task1.Wait();

        if (task1.Result.Count > 0)
        {
          command.ReplyToCommand($"{Localizer["Prefix"]} {Localizer["TestVipAlreadyClaimed"]}");
          return;
        }


        Task<bool> task2 = Task.Run(() => HasVipGroupOnDatabase(steamid, Config.VipTest.Group, serverID));

        task2.Wait();

        if (task2.Result)
        {
          command.ReplyToCommand($"{Localizer["Prefix"]} {Localizer["AlreadyNormalVip"]}");
          return;
        }

        var endAt = Config.VipTest.Time == 0 ? 0 : DateTimeOffset.UtcNow.AddMinutes(Config.VipTest.Time).ToUnixTimeMilliseconds() / 1000;
        var createdAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000;

        Task task3 = Task.Run(() =>
        {
          SetRoleOnVipManagerDatabase(name, steamid, Config.VipTest.Group, serverID, createdAt, endAt);

          query = $"INSERT INTO `{Config.Database.PrefixTestVip}` (`name`, `steamid`, `server_id`, `created_at`,`end_at`) VALUES(@name, @steamid, @serverID, @createdAt, @endAt)";

          _ = ExecuteAsync(query, new { name, steamid, serverID, createdAt, endAt });

        });

        task3.Wait();

        //ReloadUserPermissions(ulong.Parse(steamid));

        command.ReplyToCommand($"{Localizer["Prefix"]} {Localizer["TestVipActivated", Config.VipTest.Time]}");

      }
      catch (Exception e)
      {
        Logger.LogError($"{Localizer["Prefix"]} {Localizer["InternalError"]} " + e.Message);
        command.ReplyToCommand($"{Localizer["Prefix"]} {Localizer["InternalError"]}");
      }


      command.ReplyToCommand($"{Localizer["Prefix"]} {Localizer["CoolDown", Config.CooldownRefreshCommandSeconds]}");
    }
  }
}