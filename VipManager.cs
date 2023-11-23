using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Entities;
using MySqlConnector;
using Dapper;


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
        string createTable1 = "CREATE TABLE IF NOT EXISTS `vip_manager` (`id` INT NOT NULL AUTO_INCREMENT, `steamid` varchar(64) NOT NULL, `groups` varchar(200) NOT NULL, PRIMARY KEY (`id`)) ENGINE=InnoDB DEFAULT CHARSET=utf8mb3 COLLATE=utf8mb3_unicode_ci";


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
}