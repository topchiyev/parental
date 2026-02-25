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

        if (StartTime > EndTime)
            return false;

        var localDate = DateTimeOffset.FromUnixTimeSeconds(date).ToLocalTime().ToUnixTimeMilliseconds();
        var time = localDate % (24 * 3600);
        var includes = time >= StartTime && time <= EndTime;

        return includes;
    }
}