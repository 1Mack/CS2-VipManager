using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.Menu;
using Dapper;
using MySqlConnector;

namespace VipManager;

public partial class VipManager
{
  [CommandHelper(minArgs: 3, usage: "[steamid64 (without #css/)] [group] [time (minutes) or 0 (permanent)]", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
  public async void SetAdmin(CCSPlayerController? player, CommandInfo command)
  {

    if (!string.IsNullOrEmpty(Config.Commands.AddPermission) && !AdminManager.PlayerHasPermissions(player, Config.Commands.AddPermission.Split(";")))
    {
      command.ReplyToCommand($"{Config.Prefix} {ParseConfigMessage(Config.Messages.MissingCommandPermission, player)}");
      return;
    }
    string[] args = command.ArgString.Split(" ");

    GetPlayerClass? targetPlayer = GetPlayer(args[0], command);

    if (targetPlayer == null) return;

    try
    {
      using var connection = new MySqlConnection(DatabaseConnectionString);
      await connection.OpenAsync();

      string query = "SELECT id FROM vip_manager WHERE steamid = @steamid AND `group` = @group";

      args[1] = args[1].Replace("#css/", "");

      IEnumerable<dynamic> result = await connection.QueryAsync(query, new { steamid = targetPlayer.Steamid, group = args[1] });


      if (result != null && result.AsList().Count > 0)
      {
        command.ReplyToCommand($"{Config.Prefix} {ParseConfigMessage(Config.Messages.AlreadyRegistryWithSteamidAndGroup, player)}");
        return;
      }
      var endAt = args[2] == "0" ? 0 : DateTimeOffset.UtcNow.AddMinutes(int.Parse(args[2])).ToUnixTimeMilliseconds() / 1000;
      var createdAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000;

      query = $"INSERT INTO `{Config.Database.PrefixVipManager}` (`name`, `steamid`, `group`, `created_at`,`end_at`) VALUES(@name, @steamid, @group, @createdAt, @endAt)";

      await connection.ExecuteAsync(query, new { name = targetPlayer.Name, steamid = targetPlayer.Steamid, group = args[1], createdAt, endAt });

      ReloadUserPermissions(args[0], args[1], "add", int.Parse(args[2]));

      command.ReplyToCommand($"{Config.Prefix} {ParseConfigMessage(Config.Messages.AdminAddSuccess, player)}");

      await connection.CloseAsync();
    }
    catch (Exception e)
    {
      Console.WriteLine(e);
      command.ReplyToCommand($"{Config.Prefix} {ParseConfigMessage(Config.Messages.InternalError, player)}");
      return;
    }
  }
  [CommandHelper(minArgs: 2, usage: "[steamid64] [group]", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
  public async void RemoveAdmin(CCSPlayerController? player, CommandInfo command)
  {
    if (player == null || !player.IsValid) return;

    if (!string.IsNullOrEmpty(Config.Commands.RemovePermission) && !AdminManager.PlayerHasPermissions(player, Config.Commands.RemovePermission.Split(";")))
    {
      command.ReplyToCommand($"{Config.Prefix} {ParseConfigMessage(Config.Messages.MissingCommandPermission, player)}");
      return;
    }
    string[] args = command.ArgString.Split(" ");

    GetPlayerClass? targetPlayer = GetPlayer(args[0], command);

    if (targetPlayer == null) return;

    args[1] = args[1].Replace("#css/", "");

    try
    {
      using var connection = new MySqlConnection(DatabaseConnectionString);
      await connection.OpenAsync();

      string query = $"SELECT id FROM `{Config.Database.PrefixVipManager}` WHERE steamid = @steamid AND `group` = @group";

      IEnumerable<dynamic> result = await connection.QueryAsync(query, new { steamid = targetPlayer.Steamid, group = args[1] });


      if (result == null || result.AsList().Count == 0)
      {
        command.ReplyToCommand($"{Config.Prefix} {ParseConfigMessage(Config.Messages.NoAdminWithSteamidAndGroup, player)}");
        return;
      }

      query = $"DELETE FROM `{Config.Database.PrefixVipManager}` WHERE steamid = @steamid AND `group` = @group";

      await connection.ExecuteAsync(query, new { steamid = targetPlayer.Steamid, group = args[1] });

      ReloadUserPermissions(args[0], args[1], "remove");

      command.ReplyToCommand($"{Config.Prefix} {ParseConfigMessage(Config.Messages.AdminDeleteSuccess, player)}");

      await connection.CloseAsync();
    }
    catch (Exception e)
    {
      Console.WriteLine(e);
      command.ReplyToCommand($"{Config.Prefix} {ParseConfigMessage(Config.Messages.InternalError, player)}");
      return;
    }
  }

  [RequiresPermissions("#css/admin")]
  public void ReloadAdmins(CCSPlayerController? player, CommandInfo command)
  {
    if (player == null || !player.IsValid) return;

    if (!string.IsNullOrEmpty(Config.Commands.ReloadPermission) && !AdminManager.PlayerHasPermissions(player, Config.Commands.ReloadPermission.Split(";")))
    {
      command.ReplyToCommand($"{Config.Prefix} {ParseConfigMessage(Config.Messages.MissingCommandPermission, player)}");
      return;
    }
    if (DateTime.UtcNow >= reloadCommandCooldown.AddSeconds(60))
    {

      GetAdminsFromDatabase();
      reloadCommandCooldown = DateTime.UtcNow;

      command.ReplyToCommand($"{Config.Prefix} {ParseConfigMessage(Config.Messages.AdminReloadSuccess, player)}");

      return;
    }

    command.ReplyToCommand($"{Config.Prefix} {ParseConfigMessage(Config.Messages.CoolDown, player)}");

  }

  public async void TesteVip(CCSPlayerController? player, CommandInfo command)
  {
    if (player == null || !player.IsValid) return;

    if (!string.IsNullOrEmpty(Config.Commands.TestPermission) && !AdminManager.PlayerHasPermissions(player, Config.Commands.TestPermission.Split(";")))
    {
      command.ReplyToCommand($"{Config.Prefix} {ParseConfigMessage(Config.Messages.MissingCommandPermission, player)}");
      return;
    }

    int playerIndex = (int)player.Index;

    if (commandCooldown != null && DateTime.UtcNow >= commandCooldown[playerIndex].AddSeconds(Config.CooldownRefreshCommandSeconds))
    {
      commandCooldown[playerIndex] = DateTime.UtcNow;

      if (Config.VipTest.Time == 0 || Config.VipTest.Group.Length == 0)
      {
        command.ReplyToCommand($"{Config.Prefix} {ParseConfigMessage(Config.Messages.CommandBlocked, player)}!");
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
        command.ReplyToCommand($"{Config.Prefix} {ParseConfigMessage(Config.Messages.TestVipAlreadyClaimed, player)}");
        return;
      }

      query = "SELECT id FROM vip_manager WHERE steamid = @steamid and `group` = @group";

      result = await connection.QueryAsync(query, new { steamid = steamid64, group = Config.VipTest.Group });


      if (result != null && result.AsList().Count > 0)
      {
        command.ReplyToCommand($"{Config.Prefix} {ParseConfigMessage(Config.Messages.AlreadyNormalVip, player)}");
        return;
      }

      var endAt = Config.VipTest.Time == 0 ? 0 : DateTimeOffset.UtcNow.AddMinutes(Config.VipTest.Time).ToUnixTimeMilliseconds() / 1000;
      var createdAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000;

      query = $"INSERT INTO `{Config.Database.PrefixTestVip}` (`name`, `steamid`, `created_at`,`end_at`) VALUES(@name, @steamid, @createdAt, @endAt)";

      await connection.ExecuteAsync(query, new { name = player.PlayerName, steamid = steamid64, time = Config.VipTest.Time, createdAt, endAt });

      query = $"INSERT INTO `{Config.Database.PrefixVipManager}` (`name`, `steamid`, `group`, `created_at`, `end_at`) VALUES(@name, @steamid, @group, @createdAt, @endAt)";

      await connection.ExecuteAsync(query, new { name = player.PlayerName, steamid = steamid64, group = Config.VipTest.Group, createdAt, endAt });

      ReloadUserPermissions(steamid64, Config.VipTest.Group, "add", Config.VipTest.Time);

      command.ReplyToCommand($"{Config.Prefix} {ParseConfigMessage(Config.Messages.TestVipActivated, player)}");
      return;
    }


    command.ReplyToCommand($"{Config.Prefix} {ParseConfigMessage(Config.Messages.CoolDown, player)}");

  }

  [RequiresPermissions("#css/vip")]
  public void StatusVip(CCSPlayerController? player, CommandInfo command)
  {

    if (player == null || !player.IsValid) return;

    if (!string.IsNullOrEmpty(Config.Commands.StatusPermission) && !AdminManager.PlayerHasPermissions(player, Config.Commands.StatusPermission.Split(";")))
    {
      command.ReplyToCommand($"{Config.Prefix} {ParseConfigMessage(Config.Messages.MissingCommandPermission, player)}");
      return;
    }
    if (player == null || !player.IsValid) return;

    int playerIndex = (int)player.Index;

    if (commandCooldown != null && DateTime.UtcNow >= commandCooldown[playerIndex].AddSeconds(Config.CooldownRefreshCommandSeconds))
    {
      commandCooldown[playerIndex] = DateTime.UtcNow;

      string Steamid = new SteamID(player.SteamID).SteamId64.ToString();

      var findPlayerAdmins = PlayerAdmins.FindAll(obj => obj.SteamId == Steamid);

      if (findPlayerAdmins == null)
      {
        command.ReplyToCommand($"{Config.Prefix} {ParseConfigMessage(Config.Messages.NoAdminsRole, player)}");
        return;
      }
      var chatMenu = new ChatMenu(Config.Messages.RolesMenu);

      void handleMenu(CCSPlayerController player, ChatMenuOption option)
      {
        var getPlayer = findPlayerAdmins.Find(obj => obj.Group == option.Text.ToLower());
        if (getPlayer == null)
        {
          player.PrintToChat($"{Config.Prefix} {ParseConfigMessage(Config.Messages.RoleNotFound, player)}");
          return;
        }
        player.PrintToChat(ParseConfigMessage(Config.Messages.Status, player, null, getPlayer.Group, getPlayer.CreatedAt, getPlayer.EndAt));
      }


      foreach (var item in findPlayerAdmins)
      {
        chatMenu.AddMenuOption(item.Group.ToUpper(), handleMenu);

      }
      ChatMenus.OpenMenu(player, chatMenu);
    }
    else
    {
      command.ReplyToCommand($"{Config.Prefix} {ParseConfigMessage(Config.Messages.CoolDown, player)}");

    }

  }
  private void ReloadUserPermissions(string steamId64, string group, string type, int? time = null)
  {

    SteamID.TryParse(steamId64, out SteamID? steamid);

    if (steamid == null) return;

    if (type == "add" && time != null)
    {
      if (PlayerAdmins.Find(obj => obj.SteamId == steamId64 && obj.Group == group) == null)
      {
        var endAt = time == 0 ? 0 : DateTimeOffset.UtcNow.AddMinutes((int)time).ToUnixTimeMilliseconds() / 1000;
        var createdAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000;
        PlayerAdminsClass playerAdminFormat = new() { SteamId = steamId64, Group = group, CreatedAt = ParseDateTime(createdAt.ToString()), EndAt = ParseDateTime(endAt.ToString()) };

        PlayerAdmins.Add(playerAdminFormat);
      }

      AdminManager.AddPlayerToGroup(steamid, PlayerAdmins.FindAll(obj => obj.SteamId == steamId64).Select(obj => $"#css/{obj.Group}").ToArray());
    }
    else if (type == "remove")
    {
      if (PlayerAdmins.Find(obj => obj.SteamId == steamId64 && obj.Group == group) == null)
      {
        PlayerAdmins.RemoveAll(obj => obj.SteamId == steamId64 && obj.Group == group);
      }

      AdminManager.RemovePlayerFromGroup(steamid, true, $"#css/{group}");

    }
  }
}
