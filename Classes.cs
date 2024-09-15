
namespace VipManager;
public partial class VipManager
{
  public class PlayerAdminsClass
  {
    public required string Group { get; set; }
    public required string CreatedAt { get; set; }
    public required string EndAt { get; set; }
  }
  public class GetPlayerClass
  {
    public string? Name { get; set; }
    public required string Steamid { get; set; }
  }
  public class GroupsClass
  {
    public required int id { get; set; }
    public required string name { get; set; }
    public required string flags { get; set; }
    public required int immunity { get; set; }

  }
  public class AdminsDatabaseClass
  {
    public required int id { get; set; }
    public required string name { get; set; }
    public required string steamid { get; set; }
    public required string group { get; set; }
    public required int server_id { get; set; }
    public required string discord_id { get; set; }
    public required int created_at { get; set; }
    public required int end_at { get; set; }

    public static implicit operator AdminsDatabaseClass(List<dynamic> v)
    {
      throw new NotImplementedException();
    }
  }
}