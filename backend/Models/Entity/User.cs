using Parental.Backend.Models.Enum;

namespace Parental.Backend.Models.Entity;

public class User: IDbEntity
{
    public string Username { get; set; }
    public string Password { get; set; }
    public UserRoleType RoleType { get; set; }
}
