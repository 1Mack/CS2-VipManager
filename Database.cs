using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Utils;
using Dapper;
using MySqlConnector;

namespace VipManager;

public partial class VipManager
{
  private void BuildDatabaseConnectionString()
  {
    var builder = new MySqlConnectionStringBuilder
    {
      Server = Config.Database.Host,
      UserID = Config.Database.User,
      Password = Config.Database.Password,
      Database = Config.Database.Name,
      Port = (uint)Config.Database.Port,
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
        throw new Exception($"{Localizer["Prefix"]} Unable connect to database!");
      }
    }
    catch (Exception ex)
    {
      throw new Exception($"{Localizer["Prefix"]} Unknown mysql exception! " + ex.Message);
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
        string createTable1 = @$"CREATE TABLE IF NOT EXISTS `{Config.Database.PrefixVipManager}` 
        (`id` INT NOT NULL AUTO_INCREMENT, `name` varchar(128), `steamid` varchar(64) NOT NULL, `group` varchar(200) NOT NULL,
         `discord_id` varchar(100), `created_at` INT NOT NULL, end_at INT NOT NULL, PRIMARY KEY (`id`)) 
         ENGINE=InnoDB DEFAULT CHARSET=utf8mb3 COLLATE=utf8mb3_unicode_ci";

        string createTable2 = @$"CREATE TABLE IF NOT EXISTS `{Config.Database.PrefixTestVip}` 
        (`id` INT NOT NULL AUTO_INCREMENT, `name` varchar(128), `steamid` varchar(64) NOT NULL, 
        `created_at` INT NOT NULL, end_at INT NOT NULL, PRIMARY KEY (`id`)) 
        ENGINE=InnoDB DEFAULT CHARSET=utf8mb3 COLLATE=utf8mb3_unicode_ci";


        await connection.ExecuteAsync(createTable1, transaction: transaction);
        await connection.ExecuteAsync(createTable2, transaction: transaction);

        await transaction.CommitAsync();
        await connection.CloseAsync();
      }
      catch (Exception)
      {
        await transaction.RollbackAsync();
        await connection.CloseAsync();
        throw new Exception($"{Localizer["Prefix"]} Unable to create tables!");
      }
    }
    catch (Exception ex)
    {
      throw new Exception($"{Localizer["Prefix"]} Unknown mysql exception! " + ex.Message);
    }
  }

  async private void GetAdminsFromDatabase()
  {
    try
    {
      using var connection = new MySqlConnection(DatabaseConnectionString);

      await connection.OpenAsync();

      var endAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000;

      string query = $"DELETE FROM `{Config.Database.PrefixVipManager}` WHERE end_at <= @endAt AND end_at != 0";

      await connection.ExecuteAsync(query, new { endAt });

      query = $"select * from `{Config.Database.PrefixVipManager}` order by steamid ";

      var queryResult = await connection.QueryAsync(query);
      await connection.CloseAsync();

      PlayerAdmins.Clear();
      if (queryResult != null)
      {

        Server.NextFrame(() =>
        {

          List<dynamic> queryResultList = queryResult.ToList();

          foreach (var result in queryResult.ToList())
          {

            if (result.group.Length == 0) Console.WriteLine($"{Localizer["Prefix"]} there is a wrong value at  {result.steamid} - {result.group}");

            PlayerAdminsClass playerAdminFormat = new() { SteamId = result.steamid, Group = result.group, EndAt = $"{ParseDateTime($"{result.end_at}")}", CreatedAt = $"{ParseDateTime($"{result.created_at}")}" };
            PlayerAdmins.Add(playerAdminFormat);
          }

          foreach (var player in Utilities.GetPlayers())
          {
            if (player != null && player.IsValid && !player.IsBot)
            {
              var findPlayerAdmin = PlayerAdmins.FindAll(obj => obj.SteamId == player.SteamID.ToString());
              if (findPlayerAdmin != null)
              {
                AdminManager.AddPlayerToGroup(player, findPlayerAdmin.Select(obj => $"#css/{obj.Group}").ToArray());
              }
            }
          };
        });
      }

    }
    catch (Exception e)
    {
      Console.WriteLine(e);
      Server.PrintToConsole($"{Localizer["Prefix"]} Erro on loading admins" + e);
    }

  }
}
