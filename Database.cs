using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using Dapper;
using MySqlConnector;

namespace VipManager
{
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
          string createTable1 = $"CREATE TABLE IF NOT EXISTS `{Config.Database.PrefixVipManager}` (`id` INT NOT NULL AUTO_INCREMENT, `steamid` varchar(64) NOT NULL, `groups` varchar(200) NOT NULL, `discord_id` varchar(100), `timestamp` timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP, end_date timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP, PRIMARY KEY (`id`)) ENGINE=InnoDB DEFAULT CHARSET=utf8mb3 COLLATE=utf8mb3_unicode_ci";
          string createTable2 = $"CREATE TABLE IF NOT EXISTS `{Config.Database.PrefixTestVip}` (`id` INT NOT NULL AUTO_INCREMENT, `steamid` varchar(64) NOT NULL, `claimed_at` timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP, end_date timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP, PRIMARY KEY (`id`)) ENGINE=InnoDB DEFAULT CHARSET=utf8mb3 COLLATE=utf8mb3_unicode_ci";


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
    }

    async private void GetAdminsFromDatabase()
    {
      try
      {
        using var connection = new MySqlConnection(DatabaseConnectionString);

        await connection.OpenAsync();

        string query = $"DELETE FROM `{Config.Database.PrefixVipManager}` WHERE end_date <= NOW()";

        await connection.ExecuteAsync(query);

        query = $"select * from `{Config.Database.PrefixVipManager}` order by steamid ";

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
        for (int i = 1; i <= Server.MaxPlayers; i++)
        {
          CCSPlayerController playerFromIndex = Utilities.GetPlayerFromIndex(i);
          if (playerFromIndex != null && playerFromIndex.IsValid && playerFromIndex.UserId != -1)
          {
            var findPlayerAdmin = PlayerAdmins.FindAll(obj => obj.SteamId == playerFromIndex.SteamID.ToString());
            if (findPlayerAdmin != null)
            {
              AdminManager.AddPlayerToGroup(playerFromIndex, findPlayerAdmin.Select(obj => obj.Groups).ToArray());
            }
          }
        }
        await connection.CloseAsync();

      }
      catch (Exception e)
      {
        Server.PrintToConsole($"{Config.Prefix} Erro on loading admins" + e);
      }

    }
  }
}