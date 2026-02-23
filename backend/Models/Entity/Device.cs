using System;
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

    public bool IsLocked()
    {
        if (IsManuallyLocked)
            return true;

        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        
        foreach (var lockedRange in LockedRanges)
        {
            if (lockedRange.Includes(now))
                return true;
        }
        
        return false;
    }
}