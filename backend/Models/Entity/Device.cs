using System.Collections.Generic;

namespace Parental.Backend.Models.Entity;

public class Device: IDbEntity
{
    public string Id { get; set; }
    public string Username { get; set; }
    public string Name { get; set; }
    public long LastHandshakeOn { get; set; }
    public bool IsManuallyLocked { get; set; }
    public List<TimeRange> LockedRanges { get; set; } = new List<TimeRange>();
}