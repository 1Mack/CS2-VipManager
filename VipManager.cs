using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Entities;
using MySqlConnector;
using Dapper;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Modules.Menu;

namespace VipManager;

public interface IPlayerAdmins
{
  string SteamId { get; set; }
  string Groups { get; set; }
  string Timestamp { get; set; }
  string EndDate { get; set; }

}

public class PlayerAdminsClass : IPlayerAdmins
{
  public required string SteamId { get; set; }
  public required string Groups { get; set; }
  public required string Timestamp { get; set; }
  public required string EndDate { get; set; }
  // Outras propriedades e métodos...
}
public class VipManager : BasePlugin, IPluginConfig<VipManagerConfig>
{
  public override string ModuleName => "VipManager";
  public override string ModuleDescription => "Set admin by database";
  public override string ModuleAuthor => "1MaaaaaacK";
  public override string ModuleVersion => "1.1";
  public required VipManagerConfig Config { get; set; }

  private string DatabaseConnectionString = string.Empty;

  private readonly List<IPlayerAdmins> PlayerAdmins = new();
  private DateTime reloadCommandCooldown = new();
  private DateTime[] commandCooldown = new DateTime[Server.MaxPlayers];
  public override void Load(bool hotReload)
  {
    RegisterListener<Listeners.OnClientPutInServer>(OnClientPutInServer);
    RegisterListener<Listeners.OnMapStart>(OnMapStart);
    BuildDatabaseConnectionString();
    TestDatabaseConnection();
  }
  private void OnClientPutInServer(int playerSlot)
  {

    CCSPlayerController player = new(NativeAPI.GetEntityFromIndex((int)(uint)playerSlot + 1));
    var steamId = new SteamID(player.SteamID);

    var findPlayerAdmins = PlayerAdmins.FindAll(obj => obj.SteamId == steamId.SteamId64.ToString());

    if (findPlayerAdmins != null)
    {
      AdminManager.AddPlayerToGroup(steamId, findPlayerAdmins.Select(obj => obj.Groups).ToArray());

    }

  }
  private void OnMapStart(string mapName)
  {
    TestDatabaseConnection();
  }

  private void BuildDatabaseConnectionString()
  {
    var builder = new MySqlConnectionStringBuilder
    {
      Server = Config.DatabaseHost,
      UserID = Config.DatabaseUser,
      Password = Config.DatabasePassword,
      Database = Config.DatabaseName,
      Port = (uint)Config.DatabasePort,
    };

    DatabaseConnectionString = builder.ConnectionString;
  }

  private void TestDatabaseConnection()
  {
    try
    {
      using var connection = new MySqlConnection(DatabaseConnectionString);
      connection.Open();

      if (connection.State != System.Data.ConnectionState.Open)
      {
        throw new Exception($"{Config.Prefix} Unable connect to database!");
      }
    }
    catch (Exception ex)
    {
      throw new Exception($"{Config.Prefix} Unknown mysql exception! " + ex.Message);
    }
    CheckDatabaseTables();
  }

  async private void CheckDatabaseTables()
  {
    try
    {
      using var connection = new MySqlConnection(DatabaseConnectionString);
      await connection.OpenAsync();

      using var transaction = await connection.BeginTransactionAsync();

      try
      {
        string createTable1 = "CREATE TABLE IF NOT EXISTS `vip_manager` (`id` INT NOT NULL AUTO_INCREMENT, `steamid` varchar(64) NOT NULL, `groups` varchar(200) NOT NULL, `discord_id` varchar(100), `timestamp` timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP, end_date timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP, PRIMARY KEY (`id`)) ENGINE=InnoDB DEFAULT CHARSET=utf8mb3 COLLATE=utf8mb3_unicode_ci";
        string createTable2 = "CREATE TABLE IF NOT EXISTS `vip_manager_testvip` (`id` INT NOT NULL AUTO_INCREMENT, `steamid` varchar(64) NOT NULL, `claimed_at` timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP, end_date timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP, PRIMARY KEY (`id`)) ENGINE=InnoDB DEFAULT CHARSET=utf8mb3 COLLATE=utf8mb3_unicode_ci";


        await connection.ExecuteAsync(createTable1, transaction: transaction);
        await connection.ExecuteAsync(createTable2, transaction: transaction);

        await transaction.CommitAsync();
        await connection.CloseAsync();
      }
      catch (Exception)
      {
        await transaction.RollbackAsync();
        await connection.CloseAsync();
        throw new Exception($"{Config.Prefix} Unable to create tables!");
      }
    }
    catch (Exception ex)
    {
      throw new Exception($"{Config.Prefix} Unknown mysql exception! " + ex.Message);
    }

    try
    {
      GetAdminsFromDatabase();

    }
    catch (Exception)
    {
      throw new Exception($"{Config.Prefix} Couldn't read table! ");
    }
  }

  public void OnConfigParsed(VipManagerConfig config)
  {
    if (config.DatabaseHost.Length < 1 || config.DatabaseName.Length < 1 || config.DatabaseUser.Length < 1)
    {
      throw new Exception($"You need to setup Database credentials in config!");
    }
    Config = config;
  }
  async private void GetAdminsFromDatabase()
  {
    try
    {
      using var connection = new MySqlConnection(DatabaseConnectionString);

      await connection.OpenAsync();

      string query = "DELETE FROM `vip_manager` WHERE end_date <= NOW()";

      await connection.ExecuteAsync(query);

      query = "select * from `vip_manager` order by steamid ";

      var queryResult = await connection.QueryAsync(query);

      PlayerAdmins.Clear();

      if (queryResult != null)
      {
        List<dynamic> queryResultList = queryResult.ToList();

        foreach (var result in queryResult.ToList())
        {

          if (result.groups.Length == 0) Console.WriteLine($"{Config.Prefix} there is a wrong value at  {result.steamid} - {result.groups}");
          PlayerAdminsClass playerAdminFormat = new() { SteamId = result.steamid, Groups = result.groups, EndDate = $"{result.end_date}", Timestamp = $"{result.timestamp}" };
          PlayerAdmins.Add(playerAdminFormat);
        }
      }
      await connection.CloseAsync();
    }
    catch (Exception e)
    {
      Server.PrintToConsole($"{Config.Prefix} Erro on loading admins" + e);
    }

  }

  [ConsoleCommand("css_vm_add", "Set Admin")]
  [CommandHelper(minArgs: 3, usage: "[steamid64] [group] [time (minutes)]", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
  [RequiresPermissions("#css/admin")]
  public async void SetAdmin(CCSPlayerController? player, CommandInfo command)
  {
    string[] args = command.ArgString.Split(" ");

    try
    {
      using var connection = new MySqlConnection(DatabaseConnectionString);
      await connection.OpenAsync();

      string query = "SELECT id FROM vip_manager WHERE steamid = @steamid AND `groups` = @groups";

      IEnumerable<dynamic> result = await connection.QueryAsync(query, new { steamid = args[0], groups = args[1] });


      if (result != null && result.AsList().Count > 0)
      {
        command.ReplyToCommand($"{Config.Prefix} There is already a registry with this steamid and group!");
        return;
      }

      query = $"INSERT INTO `vip_manager` (`steamid`, `groups`, `end_date`) VALUES(@steamid, @groups, DATE_ADD(NOW(), INTERVAL @time MINUTE))";

      await connection.ExecuteAsync(query, new { steamid = args[0], groups = args[1], time = args[2] });

      ReloadUserPermissions(args[0], "add", command);

      command.ReplyToCommand($"{Config.Prefix} Admin has been added");

      await connection.CloseAsync();
    }
    catch (Exception e)
    {
      Console.WriteLine(e);
      command.ReplyToCommand($"{Config.Prefix} There is already a registry with this steamid and group!");
      return;
    }
  }

  [ConsoleCommand("css_vm_remove", "Remove Admin")]
  [CommandHelper(minArgs: 2, usage: "[steamid64] [group]", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
  [RequiresPermissions("#css/admin")]
  public async void RemoveAdmin(CCSPlayerController? player, CommandInfo command)
  {
    string[] args = command.ArgString.Split(" ");

    try
    {
      using var connection = new MySqlConnection(DatabaseConnectionString);
      await connection.OpenAsync();

      string query = "SELECT id FROM vip_manager WHERE steamid = @steamid AND `groups` = @groups";

      IEnumerable<dynamic> result = await connection.QueryAsync(query, new { steamid = args[0], groups = args[1] });


      if (result == null || result.AsList().Count == 0)
      {
        command.ReplyToCommand($"{Config.Prefix} There is no admin with this steamID and group!");
        return;
      }

      query = $"DELETE FROM `vip_manager` WHERE steamid = @steamid AND `groups` = @groups";

      await connection.ExecuteAsync(query, new { steamid = args[0], groups = args[1] });

      ReloadUserPermissions(args[0], "remove", command);

      command.ReplyToCommand($"{Config.Prefix} Admin has been deleted");

      await connection.CloseAsync();
    }
    catch (Exception e)
    {
      Console.WriteLine(e);
      command.ReplyToCommand($"{Config.Prefix} There is already a registry with this steamid and group!");
      return;
    }
  }

  [ConsoleCommand($"css_vm_reload", "Reload Admins")]
  [RequiresPermissions("#css/admin")]
  public void ReloadAdmins(CCSPlayerController? player, CommandInfo command)
  {

    if (DateTime.UtcNow >= reloadCommandCooldown.AddSeconds(60))
    {

      GetAdminsFromDatabase();
      reloadCommandCooldown = DateTime.UtcNow;

      command.ReplyToCommand($"{Config.Prefix} Admins reloaded successfully");

      return;
    }

    command.ReplyToCommand($"{Config.Prefix} You are on a cooldown...wait 60 seconds and try again");

  }

  [ConsoleCommand($"css_vm_test", "Test VIP")]
  public async void TesteVip(CCSPlayerController? player, CommandInfo command)
  {
    if (player == null || !player.IsValid) return;

    int playerIndex = (int)player.EntityIndex!.Value.Value;

    if (commandCooldown != null && DateTime.UtcNow >= commandCooldown[playerIndex].AddSeconds(Config.CooldownRefreshCommandSeconds))
    {
      commandCooldown[playerIndex] = DateTime.UtcNow;

      if (Config.VipTestTime == 0 || Config.VipTestGroup.Length == 0)
      {
        command.ReplyToCommand($"{Config.Prefix} This command is blocked by the server!");
        return;
      }

      using var connection = new MySqlConnection(DatabaseConnectionString);
      await connection.OpenAsync();

      string query = "SELECT id FROM vip_manager_testvip WHERE steamid = @steamid";

      var steamid = new SteamID(player.SteamID);

      string steamid64 = steamid.SteamId64.ToString();

      IEnumerable<dynamic> result = await connection.QueryAsync(query, new { steamid = steamid64 });

      if (result != null && result.AsList().Count > 0)
      {
        command.ReplyToCommand($"{Config.Prefix} Vou have already claimed your test vip!");
        return;
      }

      query = "SELECT id FROM vip_manager WHERE steamid = @steamid and `groups` = @groups";

      result = await connection.QueryAsync(query, new { steamid = steamid64, groups = Config.VipTestGroup });


      if (result != null && result.AsList().Count > 0)
      {
        command.ReplyToCommand($"{Config.Prefix} Vou have already a normal vip!");
        return;
      }

      query = "INSERT INTO `vip_manager_testvip` (`steamid`, `end_date`) VALUES(@steamid, DATE_ADD(NOW(), INTERVAL @time MINUTE))";

      await connection.ExecuteAsync(query, new { steamid = steamid64, time = Config.VipTestTime });

      query = "INSERT INTO `vip_manager` (`steamid`, `groups`, `end_date`) VALUES(@steamid, @groups, DATE_ADD(NOW(), INTERVAL @time MINUTE))";

      await connection.ExecuteAsync(query, new { steamid = steamid64, groups = Config.VipTestGroup, time = Config.VipTestTime });

      ReloadUserPermissions(steamid64, "add", command, Config.VipTestGroup);

      command.ReplyToCommand($"{Config.Prefix} You have activated your VIP successfully for {Config.VipTestTime} minutes");
      return;
    }


    command.ReplyToCommand($"{Config.Prefix} You are on a cooldown...wait {Config.CooldownRefreshCommandSeconds} seconds and try again");

  }

  [ConsoleCommand("css_vm_status", "Check your vip time left")]
  [RequiresPermissions("#css/vip")]
  public void StatusVip(CCSPlayerController? player, CommandInfo command)
  {

    if (player == null || !player.IsValid) return;

    int playerIndex = (int)player.EntityIndex!.Value.Value;

    if (commandCooldown != null && DateTime.UtcNow >= commandCooldown[playerIndex].AddSeconds(Config.CooldownRefreshCommandSeconds))
    {
      commandCooldown[playerIndex] = DateTime.UtcNow;

      string Steamid = new SteamID(player.SteamID).SteamId64.ToString();

      var findPlayerAdmins = PlayerAdmins.FindAll(obj => obj.SteamId == Steamid);

      if (findPlayerAdmins == null)
      {
        command.ReplyToCommand($"{Config.Prefix} You don't have any admin roles!");
        return;
      }
      var chatMenu = new ChatMenu("Your Admin Roles");

      void handleMenu(CCSPlayerController player, ChatMenuOption option)
      {
        var getPlayer = findPlayerAdmins.Find(obj => obj.Groups == option.Text.ToLower());
        if (getPlayer == null)
        {
          player.PrintToChat("Role not found. Rejoin the server and try again.");
          return;
        }

        player.PrintToChat($"------------------------------\u2029Role: {getPlayer.Groups}\u2029Created At: {getPlayer.Timestamp}\u2029End At: {getPlayer.EndDate}\u2029------------------------------");
      }


      foreach (var item in findPlayerAdmins)
      {
        chatMenu.AddMenuOption(item.Groups.ToUpper(), handleMenu);

      }
      ChatMenus.OpenMenu(player, chatMenu);
    }
    else
    {
      command.ReplyToCommand($"{Config.Prefix} You are on a cooldown...wait {Config.CooldownRefreshCommandSeconds} seconds and try again");

    }

  }

  private void ReloadUserPermissions(string steamId64, string type, CommandInfo command, string? group = null)
  {
    string[] args = command.ArgString.Split(" ");
    string arg0, arg1;
    int arg2;


    if (group != null)
    {
      arg0 = steamId64;
      arg1 = group;
      arg2 = Config.VipTestTime;
    }
    else
    {
      arg0 = args[0];
      arg1 = args[1];
      arg2 = int.Parse(args[2]);

    }

    SteamID.TryParse(steamId64, out SteamID? steamid);

    if (steamid == null) return;

    if (type == "add")
    {
      if (PlayerAdmins.Find(obj => obj.SteamId == steamId64 && obj.Groups == arg1) == null)
      {
        PlayerAdminsClass playerAdminFormat = new() { SteamId = arg0, Groups = arg1, Timestamp = DateTime.UtcNow.ToString(), EndDate = DateTime.UtcNow.AddMinutes(arg2).ToString() };

        PlayerAdmins.Add(playerAdminFormat);
      }

      AdminManager.AddPlayerToGroup(steamid, PlayerAdmins.FindAll(obj => obj.SteamId == steamId64).Select(obj => obj.Groups).ToArray());
    }
    else if (type == "remove")
    {
      if (PlayerAdmins.Find(obj => obj.SteamId == steamId64 && obj.Groups == arg1) == null)
      {
        PlayerAdmins.RemoveAll(obj => obj.SteamId == steamId64 && obj.Groups == arg1);
      }

      AdminManager.RemovePlayerFromGroup(steamid, true, arg1);

    }
  }

}