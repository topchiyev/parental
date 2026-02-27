using System;

namespace Parental.WinService.Models.Entity;

public class TimeRange
{
    public long StartTime { get; set; }
    public long EndTime { get; set; }
    public bool IsEnabled { get; set; }
    
    public bool Includes(long date)
    {
        if (!IsEnabled)
            return false;

        if (StartTime < 0 || EndTime < 0)
            return false;

        var maxSecondsInDay = 24 * 3600;
        if (StartTime >= maxSecondsInDay || EndTime >= maxSecondsInDay)
            return false;

        var localTime = DateTimeOffset.FromUnixTimeSeconds(date).ToLocalTime().TimeOfDay;
        var time = (long)localTime.TotalSeconds;

        if (StartTime <= EndTime)
            return time >= StartTime && time <= EndTime;

        // Allow ranges that span midnight (for example 23:00 -> 02:00).
        return time >= StartTime || time <= EndTime;
    }
}
