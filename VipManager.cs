using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Entities;
using MySqlConnector;
using Dapper;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API;

namespace VipManager;
public class VipManager : BasePlugin, IPluginConfig<VipManagerConfig>
{
  public override string ModuleName => "VipManager";
  public override string ModuleDescription => "Set admin by database";
  public override string ModuleAuthor => "1MaaaaaacK";
  public override string ModuleVersion => "1.0";
  public required VipManagerConfig Config { get; set; }

  private string DatabaseConnectionString = string.Empty;

  private Dictionary<string, string> PlayerAdmins = new();


  public override void Load(bool hotReload)
  {
    RegisterListener<Listeners.OnClientPutInServer>(OnClientPutInServer);
    AddCommand($"css_teste", "Skins info", (player, info) =>
    {
      if (player == null) return;
      var steamid = new SteamID(player.SteamID);
      var teste = AdminManager.GetPlayerAdminData(steamid);
      if (teste != null)
      {
        Console.WriteLine(string.Join(", ", teste.Groups));
      }
    });

    BuildDatabaseConnectionString();
    TestDatabaseConnection();
  }

  private void OnClientPutInServer(int playerSlot)
  {
    CCSPlayerController player = new CCSPlayerController(NativeAPI.GetEntityFromIndex((int)(uint)playerSlot + 1));
    var steamId = new SteamID(player.SteamID);

    if (PlayerAdmins.ContainsKey(steamId.SteamId64.ToString()))
    {
      AdminManager.AddPlayerToGroup(steamId, PlayerAdmins[steamId.SteamId64.ToString()].Split(","));

    }

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
        string createTable1 = "CREATE TABLE IF NOT EXISTS `vip_manager` (`id` INT NOT NULL AUTO_INCREMENT, `steamid` varchar(64) NOT NULL, `groups` varchar(200) NOT NULL, `discord_id` varchar(100), `timestamp` timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP, end_at timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP, PRIMARY KEY (`id`)) ENGINE=InnoDB DEFAULT CHARSET=utf8mb3 COLLATE=utf8mb3_unicode_ci";


        await connection.ExecuteAsync(createTable1, transaction: transaction);

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
    using var connection = new MySqlConnection(DatabaseConnectionString);

    await connection.OpenAsync();
    string query = "select steamid, GROUP_CONCAT(`groups`) as `groups` from `vip_manager`  GROUP BY steamid";

    var queryResult = await connection.QueryAsync(query);

    if (queryResult != null)
    {
      foreach (var result in queryResult.ToList())
      {

        if (result.groups.Length == 0) Console.WriteLine($"{Config.Prefix} there is a wrong value at  {result.steamid} - {result.groups}");

        if (!PlayerAdmins.ContainsKey(result.steamid))
        {
          PlayerAdmins.Add(result.steamid, result.groups);
        }
        else
        {
          PlayerAdmins[result.steamid] = result.groups;
        }
      }
    }
    await connection.CloseAsync();
  }

  [ConsoleCommand("css_admin_add", "Set Admin")]
  [CommandHelper(minArgs: 2, usage: "[steamid64] [group]", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
  [RequiresPermissions("#css/admin")]
  public async void SetAdmin(CCSPlayerController? player, CommandInfo command)
  {
    try
    {
      using var connection = new MySqlConnection(DatabaseConnectionString);
      await connection.OpenAsync();

      string query = "SELECT id FROM vip_manager WHERE steamid = @steamid AND `groups` = @groups";

      IEnumerable<dynamic> result = await connection.QueryAsync(query, new { steamid = command.GetArg(0), groups = command.GetArg(1) });


      if (result != null && result.AsList().Count > 0)
      {
        command.ReplyToCommand($"{Config.Prefix} There is already a registry with this steamid and group!");
        return;
      }

      query = $"INSERT INTO `vip_manager` (`steamid`, `groups`) VALUES(@steamid, @groups)";

      await connection.ExecuteAsync(query, new { steamid = command.GetArg(0), groups = command.GetArg(1) });

      ReloadUserPermissions(command.GetArg(0), "add", command.GetArg(1), command);

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

  [ConsoleCommand("css_admin_remove", "Remove Admin")]
  [CommandHelper(minArgs: 2, usage: "[steamid64] [group]", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
  [RequiresPermissions("#css/admin")]
  public async void RemoveAdmin(CCSPlayerController? player, CommandInfo command)
  {
    try
    {
      using var connection = new MySqlConnection(DatabaseConnectionString);
      await connection.OpenAsync();

      string query = "SELECT id FROM vip_manager WHERE steamid = @steamid AND `groups` = @groups";

      IEnumerable<dynamic> result = await connection.QueryAsync(query, new { steamid = command.GetArg(0), groups = command.GetArg(1) });


      if (result == null || result.AsList().Count == 0)
      {
        command.ReplyToCommand($"{Config.Prefix} There is no admin with this steamID and group!");
        return;
      }

      query = $"DELETE FROM `vip_manager` WHERE steamid = @steamid AND `groups` = @groups";

      await connection.ExecuteAsync(query, new { steamid = command.GetArg(0), groups = command.GetArg(1) });

      ReloadUserPermissions(command.GetArg(0), "remove", command.GetArg(1), command);

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

  private void ReloadUserPermissions(string steamId64, string type, string groups, CommandInfo command)
  {
    SteamID.TryParse(steamId64, out SteamID? steamid);

    if (steamid == null) return;

    if (type == "add")
    {
      if (PlayerAdmins.ContainsKey(steamId64))
      {
        if (PlayerAdmins[steamId64] != groups)
        {
          PlayerAdmins[steamId64] += groups;
        }
      }
      else
      {
        PlayerAdmins.Add(steamId64, groups);
      }

      AdminManager.AddPlayerToGroup(steamid, PlayerAdmins[steamId64].Split(","));
    }
    else if (type == "remove")
    {
      if (PlayerAdmins.ContainsKey(steamId64) && PlayerAdmins[steamId64] == groups)
      {
        PlayerAdmins.Remove(steamId64);
      }

      AdminManager.RemovePlayerFromGroup(steamid, true, groups);

    }
  }

}