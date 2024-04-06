using Dapper;
using MySqlConnector;

namespace VipManager;

public partial class VipManager
{
  private string _databaseConnectionString = string.Empty;

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

    _databaseConnectionString = builder.ConnectionString;
  }
  public async Task<List<T>> QueryAsync<T>(string query, object? parameters = null)
  {
    try
    {
      using MySqlConnection connection = new(_databaseConnectionString);

      await connection.OpenAsync();

      var queryResult = await connection.QueryAsync<T>(query, parameters);

      await connection.CloseAsync();

      return queryResult.ToList();
    }
    catch (Exception ex)
    {
      throw new Exception(ex.Message);
    }
  }
  public async Task ExecuteAsync(string query, object? parameters = null)
  {

    try
    {
      using MySqlConnection connection = new(_databaseConnectionString);

      await connection.OpenAsync();

      var queryResult = await connection.ExecuteAsync(query, parameters);

      await connection.CloseAsync();
    }
    catch (Exception ex)
    {
      throw new Exception(ex.Message);
    }
  }

}
