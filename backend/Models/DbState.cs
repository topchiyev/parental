using System.Collections.Generic;

namespace Parental.Backend.Models;

public class DbState
{
    public long UpdatedOn { get; set; }
    public List<IDbEntity> Entities { get; set; } = new List<IDbEntity>();
}