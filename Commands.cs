using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.Menu;
using Dapper;
using MySqlConnector;

namespace VipManager
{
  public partial class VipManager
  {
    [CommandHelper(minArgs: 3, usage: "[steamid64] [group] [time (minutes)]", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    public async void SetAdmin(CCSPlayerController? player, CommandInfo command)
    {
      if (!string.IsNullOrEmpty(Config.Commands.AddPermission) && !AdminManager.PlayerHasPermissions(player, Config.Commands.AddPermission.Split(";")))
      {
        command.ReplyToCommand($"{Config.Prefix} {Config.Messages.MissingCommandPermission}");
        return;
      }
      string[] args = command.ArgString.Split(" ");

      try
      {
        using var connection = new MySqlConnection(DatabaseConnectionString);
        await connection.OpenAsync();

        string query = "SELECT id FROM vip_manager WHERE steamid = @steamid AND `groups` = @groups";

        IEnumerable<dynamic> result = await connection.QueryAsync(query, new { steamid = args[0], groups = args[1] });


        if (result != null && result.AsList().Count > 0)
        {
          command.ReplyToCommand($"{Config.Prefix} {Config.Messages.AlreadyRegistryWithSteamidAndGroup}");
          return;
        }

        query = $"INSERT INTO `{Config.Database.PrefixVipManager}` (`steamid`, `groups`, `end_date`) VALUES(@steamid, @groups, DATE_ADD(NOW(), INTERVAL @time MINUTE))";

        await connection.ExecuteAsync(query, new { steamid = args[0], groups = args[1], time = args[2] });

        ReloadUserPermissions(args[0], args[1], "add", int.Parse(args[2]));

        command.ReplyToCommand($"{Config.Prefix} {Config.Messages.AdminAddSuccess}");

        await connection.CloseAsync();
      }
      catch (Exception e)
      {
        Console.WriteLine(e);
        command.ReplyToCommand($"{Config.Prefix} {Config.Messages.InternalError}");
        return;
      }
    }
    [CommandHelper(minArgs: 2, usage: "[steamid64] [group]", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    public async void RemoveAdmin(CCSPlayerController? player, CommandInfo command)
    {
      if (!string.IsNullOrEmpty(Config.Commands.RemovePermission) && !AdminManager.PlayerHasPermissions(player, Config.Commands.RemovePermission.Split(";")))
      {
        command.ReplyToCommand($"{Config.Prefix} {Config.Messages.MissingCommandPermission}");
        return;
      }
      string[] args = command.ArgString.Split(" ");

      try
      {
        using var connection = new MySqlConnection(DatabaseConnectionString);
        await connection.OpenAsync();

        string query = $"SELECT id FROM `{Config.Database.PrefixVipManager}` WHERE steamid = @steamid AND `groups` = @groups";

        IEnumerable<dynamic> result = await connection.QueryAsync(query, new { steamid = args[0], groups = args[1] });


        if (result == null || result.AsList().Count == 0)
        {
          command.ReplyToCommand($"{Config.Prefix} {Config.Messages.NoAdminWithSteamidAndGroup}");
          return;
        }

        query = $"DELETE FROM `{Config.Database.PrefixVipManager}` WHERE steamid = @steamid AND `groups` = @groups";

        await connection.ExecuteAsync(query, new { steamid = args[0], groups = args[1] });

        ReloadUserPermissions(args[0], args[1], "remove");

        command.ReplyToCommand($"{Config.Prefix} {Config.Messages.AdminDeleteSuccess}");

        await connection.CloseAsync();
      }
      catch (Exception e)
      {
        Console.WriteLine(e);
        command.ReplyToCommand($"{Config.Prefix} {Config.Messages.InternalError}");
        return;
      }
    }

    [RequiresPermissions("#css/admin")]
    public void ReloadAdmins(CCSPlayerController? player, CommandInfo command)
    {
      if (!string.IsNullOrEmpty(Config.Commands.ReloadPermission) && !AdminManager.PlayerHasPermissions(player, Config.Commands.ReloadPermission.Split(";")))
      {
        command.ReplyToCommand($"{Config.Prefix} {Config.Messages.MissingCommandPermission}");
        return;
      }
      if (DateTime.UtcNow >= reloadCommandCooldown.AddSeconds(60))
      {

        GetAdminsFromDatabase();
        reloadCommandCooldown = DateTime.UtcNow;

        command.ReplyToCommand($"{Config.Prefix} {Config.Messages.AdminReloadSuccess}");

        return;
      }

      command.ReplyToCommand($"{Config.Prefix} {Config.Messages.CoolDown}");

    }

    public async void TesteVip(CCSPlayerController? player, CommandInfo command)
    {
      if (!string.IsNullOrEmpty(Config.Commands.TestPermission) && !AdminManager.PlayerHasPermissions(player, Config.Commands.TestPermission.Split(";")))
      {
        command.ReplyToCommand($"{Config.Prefix} {Config.Messages.MissingCommandPermission}");
        return;
      }
      if (player == null || !player.IsValid) return;

      int playerIndex = (int)player.EntityIndex!.Value.Value;

      if (commandCooldown != null && DateTime.UtcNow >= commandCooldown[playerIndex].AddSeconds(Config.CooldownRefreshCommandSeconds))
      {
        commandCooldown[playerIndex] = DateTime.UtcNow;

        if (Config.VipTest.Time == 0 || Config.VipTest.Group.Length == 0)
        {
          command.ReplyToCommand($"{Config.Prefix} {Config.Messages.CommandBlocked}!");
          return;
        }

        using var connection = new MySqlConnection(DatabaseConnectionString);
        await connection.OpenAsync();

        string query = $"SELECT id FROM `{Config.Database.PrefixTestVip}` WHERE steamid = @steamid";

        var steamid = new SteamID(player.SteamID);

        string steamid64 = steamid.SteamId64.ToString();

        IEnumerable<dynamic> result = await connection.QueryAsync(query, new { steamid = steamid64 });

        if (result != null && result.AsList().Count > 0)
        {
          command.ReplyToCommand($"{Config.Prefix} {Config.Messages.TestVipAlreadyClaimed}");
          return;
        }

        query = "SELECT id FROM vip_manager WHERE steamid = @steamid and `groups` = @groups";

        result = await connection.QueryAsync(query, new { steamid = steamid64, groups = Config.VipTest.Group });


        if (result != null && result.AsList().Count > 0)
        {
          command.ReplyToCommand($"{Config.Prefix} {Config.Messages.AlreadyNormalVip}");
          return;
        }

        query = $"INSERT INTO `{Config.Database.PrefixTestVip}` (`steamid`, `end_date`) VALUES(@steamid, DATE_ADD(NOW(), INTERVAL @time MINUTE))";

        await connection.ExecuteAsync(query, new { steamid = steamid64, time = Config.VipTest.Time });

        query = $"INSERT INTO `{Config.Database.PrefixVipManager}` (`steamid`, `groups`, `end_date`) VALUES(@steamid, @groups, DATE_ADD(NOW(), INTERVAL @time MINUTE))";

        await connection.ExecuteAsync(query, new { steamid = steamid64, groups = Config.VipTest.Group, time = Config.VipTest.Time });

        ReloadUserPermissions(steamid64, Config.VipTest.Group, "add", Config.VipTest.Time);

        command.ReplyToCommand($"{Config.Prefix} {Config.Messages.TestVipActivated}");
        return;
      }


      command.ReplyToCommand($"{Config.Prefix} {Config.Messages.CoolDown}");

    }

    [RequiresPermissions("#css/vip")]
    public void StatusVip(CCSPlayerController? player, CommandInfo command)
    {
      if (!string.IsNullOrEmpty(Config.Commands.StatusPermission) && !AdminManager.PlayerHasPermissions(player, Config.Commands.StatusPermission.Split(";")))
      {
        command.ReplyToCommand($"{Config.Prefix} {Config.Messages.MissingCommandPermission}");
        return;
      }
      if (player == null || !player.IsValid) return;

      int playerIndex = (int)player.EntityIndex!.Value.Value;

      if (commandCooldown != null && DateTime.UtcNow >= commandCooldown[playerIndex].AddSeconds(Config.CooldownRefreshCommandSeconds))
      {
        commandCooldown[playerIndex] = DateTime.UtcNow;

        string Steamid = new SteamID(player.SteamID).SteamId64.ToString();

        var findPlayerAdmins = PlayerAdmins.FindAll(obj => obj.SteamId == Steamid);

        if (findPlayerAdmins == null)
        {
          command.ReplyToCommand($"{Config.Prefix} {Config.Messages.NoAdminsRole}");
          return;
        }
        var chatMenu = new ChatMenu("Your Admin Roles");

        void handleMenu(CCSPlayerController player, ChatMenuOption option)
        {
          var getPlayer = findPlayerAdmins.Find(obj => obj.Groups == option.Text.ToLower());
          if (getPlayer == null)
          {
            player.PrintToChat($"{Config.Prefix} {Config.Messages.RoleNotFound}");
            return;
          }

          string statusMsg = Config.Messages.Status.Replace("{GROUP}", getPlayer.Groups).Replace("{TIMESTAMP", getPlayer.Timestamp).Replace("{ENDDATE}", getPlayer.EndDate);
          player.PrintToChat(statusMsg);
        }


        foreach (var item in findPlayerAdmins)
        {
          chatMenu.AddMenuOption(item.Groups.ToUpper(), handleMenu);

        }
        ChatMenus.OpenMenu(player, chatMenu);
      }
      else
      {
        command.ReplyToCommand($"{Config.Prefix} {Config.Messages.CoolDown}");

      }

    }
    private void ReloadUserPermissions(string steamId64, string group, string type, int? time = null)
    {

      SteamID.TryParse(steamId64, out SteamID? steamid);

      if (steamid == null) return;

      if (type == "add" && time != null)
      {
        if (PlayerAdmins.Find(obj => obj.SteamId == steamId64 && obj.Groups == group) == null)
        {
          PlayerAdminsClass playerAdminFormat = new() { SteamId = steamId64, Groups = group, Timestamp = DateTime.UtcNow.ToString(), EndDate = DateTime.UtcNow.AddMinutes((double)time).ToString() };

          PlayerAdmins.Add(playerAdminFormat);
        }

        AdminManager.AddPlayerToGroup(steamid, PlayerAdmins.FindAll(obj => obj.SteamId == steamId64).Select(obj => obj.Groups).ToArray());
      }
      else if (type == "remove")
      {
        if (PlayerAdmins.Find(obj => obj.SteamId == steamId64 && obj.Groups == group) == null)
        {
          PlayerAdmins.RemoveAll(obj => obj.SteamId == steamId64 && obj.Groups == group);
        }

        AdminManager.RemovePlayerFromGroup(steamid, true, group);

      }
    }
  }
}