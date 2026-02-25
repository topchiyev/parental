namespace Parental.WinService.Models.Entity;

public class TimeRange
{
    public long StartTime { get; set; }
    public long EndTime { get; set; }
    
    public bool Includes(long time)
    {
        time %= 24 * 3600;
        var startTime = StartTime % (24 * 3600);
        var endTime = EndTime % (24 * 3600);
        if (startTime > endTime)
            return false;
        
        return time >= startTime && time <= endTime;
    }
}