namespace GdeltApi.Models;

public class TimelineDataPoint
{
    public string? Date { get; set; }
    public double? Value { get; set; }
    public double? Norm { get; set; }
}

public class TimelineSeries
{
    public string? Series { get; set; }
    public List<TimelineDataPoint>? Data { get; set; }
}

public class TimelineResponse
{
    public List<TimelineSeries>? Timeline { get; set; }
}

public class TimelineSearchResponse
{
    public List<Dictionary<string, object>> Data { get; set; } = new();
}
