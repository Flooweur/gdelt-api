using System.ComponentModel.DataAnnotations;

namespace GdeltApi.Models;

public class FiltersDto
{
    public string? StartDate { get; set; }
    public string? EndDate { get; set; }
    public string? Timespan { get; set; }
    public int NumRecords { get; set; } = 250;
    public string[]? Keyword { get; set; }
    public string[]? Domain { get; set; }
    public string[]? DomainExact { get; set; }
    public string? Near { get; set; }
    public string? Repeat { get; set; }
    public string[]? Country { get; set; }
    public string[]? Language { get; set; }
    public string[]? Theme { get; set; }
    public string? Tone { get; set; }
    public string? ToneAbsolute { get; set; }
}
