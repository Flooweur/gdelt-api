using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using GdeltApi.Models;

namespace GdeltApi.Services;

public class GdeltService : IGdeltService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<GdeltService> _logger;
    private const string BaseUrl = "https://api.gdeltproject.org/api/v2/doc/doc";

    public GdeltService(HttpClient httpClient, ILogger<GdeltService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "GDELT DOC C# API client - https://github.com/alex9smith/gdelt-doc-api");
    }

    public async Task<List<Article>> ArticleSearchAsync(FiltersDto filters)
    {
        var queryString = BuildQueryString(filters);
        var response = await _httpClient.GetAsync($"{BaseUrl}?query={Uri.EscapeDataString(queryString)}&mode=artlist&format=json");
        
        response.EnsureSuccessStatusCode();
        
        var contentType = response.Content.Headers.ContentType?.MediaType ?? "";
        var content = await response.Content.ReadAsStringAsync();
        
        // Check if response is HTML (API sometimes returns HTML for invalid requests)
        if (contentType.Contains("text/html"))
        {
            throw new InvalidOperationException($"The query was not valid. The API error message was: {content.Trim()}");
        }
        
        var json = LoadJson(content);
        
        if (json.TryGetValue("articles", out var articlesToken) && articlesToken is JsonElement articlesElement)
        {
            return articlesElement.Deserialize<List<Article>>() ?? new List<Article>();
        }
        
        return new List<Article>();
    }

    public async Task<List<Dictionary<string, object>>> TimelineSearchAsync(string mode, FiltersDto filters)
    {
        var validModes = new[] { "timelinevol", "timelinevolraw", "timelinetone", "timelinelang", "timelinesourcecountry" };
        if (!validModes.Contains(mode))
        {
            throw new ArgumentException($"Mode {mode} not in supported API modes");
        }

        var queryString = BuildQueryString(filters);
        var response = await _httpClient.GetAsync($"{BaseUrl}?query={Uri.EscapeDataString(queryString)}&mode={mode}&format=json");
        
        response.EnsureSuccessStatusCode();
        
        var contentType = response.Content.Headers.ContentType?.MediaType ?? "";
        var content = await response.Content.ReadAsStringAsync();
        
        // Check if response is HTML (API sometimes returns HTML for invalid requests)
        if (contentType.Contains("text/html"))
        {
            throw new InvalidOperationException($"The query was not valid. The API error message was: {content.Trim()}");
        }
        
        var json = LoadJson(content);
        
        if (!json.TryGetValue("timeline", out var timelineElement))
        {
            return new List<Dictionary<string, object>>();
        }

        var timeline = timelineElement.Deserialize<List<TimelineSeries>>();
        if (timeline == null || timeline.Count == 0)
        {
            return new List<Dictionary<string, object>>();
        }

        var result = new List<Dictionary<string, object>>();
        var firstSeries = timeline[0];
        
        if (firstSeries.Data == null || firstSeries.Data.Count == 0)
        {
            return new List<Dictionary<string, object>>();
        }

        // Build result dictionary for each data point
        for (int i = 0; i < firstSeries.Data.Count; i++)
        {
            var dataPoint = new Dictionary<string, object>
            {
                ["datetime"] = firstSeries.Data[i].Date ?? string.Empty
            };

            // Add all series values for this data point
            foreach (var series in timeline)
            {
                if (series.Data != null && i < series.Data.Count)
                {
                    var value = series.Data[i].Value ?? 0;
                    dataPoint[series.Series ?? "Unknown"] = value;
                }
            }

            // For timelinevolraw, add the "All Articles" column
            var norm = firstSeries.Data[i].Norm;
            if (mode == "timelinevolraw" && norm.HasValue)
            {
                dataPoint["All Articles"] = norm.Value;
            }

            result.Add(dataPoint);
        }

        return result;
    }

    public async Task<List<Article>> GetLastHourAsync(FiltersDto? filters = null)
    {
        filters ??= new FiltersDto();
        filters.Timespan = "1h";
        return await ArticleSearchAsync(filters);
    }

    private string BuildQueryString(FiltersDto filters)
    {
        var queryParams = new List<string>();

        // Validate date/timespan
        if (string.IsNullOrEmpty(filters.StartDate) && string.IsNullOrEmpty(filters.EndDate) && string.IsNullOrEmpty(filters.Timespan))
        {
            throw new ArgumentException("Must provide either start_date and end_date, or timespan");
        }

        if (!string.IsNullOrEmpty(filters.StartDate) && !string.IsNullOrEmpty(filters.EndDate) && !string.IsNullOrEmpty(filters.Timespan))
        {
            throw new ArgumentException("Can only provide either start_date and end_date, or timespan");
        }

        // Keyword
        if (filters.Keyword != null && filters.Keyword.Length > 0)
        {
            if (filters.Keyword.Length == 1)
            {
                queryParams.Add($"\"{filters.Keyword[0]}\" ");
            }
            else
            {
                var keywords = filters.Keyword.Select(k => k.Contains(' ') ? $"\"{k}\"" : k);
                queryParams.Add($"({string.Join(" OR ", keywords)}) ");
            }
        }

        // Domain
        if (filters.Domain != null && filters.Domain.Length > 0)
        {
            queryParams.Add(FilterToString("domain", filters.Domain));
        }

        // Domain Exact
        if (filters.DomainExact != null && filters.DomainExact.Length > 0)
        {
            queryParams.Add(FilterToString("domainis", filters.DomainExact));
        }

        // Country
        if (filters.Country != null && filters.Country.Length > 0)
        {
            queryParams.Add(FilterToString("sourcecountry", filters.Country));
        }

        // Language
        if (filters.Language != null && filters.Language.Length > 0)
        {
            queryParams.Add(FilterToString("sourcelang", filters.Language));
        }

        // Theme
        if (filters.Theme != null && filters.Theme.Length > 0)
        {
            queryParams.Add(FilterToString("theme", filters.Theme));
        }

        // Tone
        if (!string.IsNullOrEmpty(filters.Tone))
        {
            ValidateTone(filters.Tone);
            queryParams.Add($"tone{filters.Tone} ");
        }

        // Tone Absolute
        if (!string.IsNullOrEmpty(filters.ToneAbsolute))
        {
            ValidateTone(filters.ToneAbsolute);
            queryParams.Add($"toneabs{filters.ToneAbsolute} ");
        }

        // Near
        if (!string.IsNullOrEmpty(filters.Near))
        {
            queryParams.Add(filters.Near);
        }

        // Repeat
        if (!string.IsNullOrEmpty(filters.Repeat))
        {
            queryParams.Add(filters.Repeat);
        }

        // Date range
        if (!string.IsNullOrEmpty(filters.StartDate))
        {
            if (string.IsNullOrEmpty(filters.EndDate))
            {
                throw new ArgumentException("Must provide both start_date and end_date");
            }
            queryParams.Add($"&startdatetime={FormatDate(filters.StartDate)}");
            queryParams.Add($"&enddatetime={FormatDate(filters.EndDate)}");
        }
        else if (!string.IsNullOrEmpty(filters.Timespan))
        {
            ValidateTimespan(filters.Timespan);
            queryParams.Add($"&timespan={filters.Timespan}");
        }

        // Num records
        if (filters.NumRecords > 250)
        {
            throw new ArgumentException("num_records must be 250 or less");
        }
        queryParams.Add($"&maxrecords={filters.NumRecords}");

        return string.Join("", queryParams);
    }

    private string FilterToString(string name, string[] values)
    {
        if (values.Length == 1)
        {
            return $"{name}:{values[0]} ";
        }
        return $"({string.Join(" OR ", values.Select(v => $"{name}:{v}"))}) ";
    }

    private void ValidateTone(string tone)
    {
        if (!tone.Contains('<') && !tone.Contains('>'))
        {
            throw new ArgumentException("Tone must contain either greater than or less than");
        }
        if (tone.Contains('='))
        {
            throw new ArgumentException("Tone cannot contain '='");
        }
    }

    private void ValidateTimespan(string timespan)
    {
        var validUnits = new[] { "min", "h", "hours", "d", "days", "w", "weeks", "m", "months" };
        var match = Regex.Match(timespan, @"^(\d+)([a-z]+)$");
        
        if (!match.Success)
        {
            throw new ArgumentException($"Timespan {timespan} is invalid. Could not parse value and unit.");
        }

        var unit = match.Groups[2].Value;
        if (!validUnits.Contains(unit))
        {
            throw new ArgumentException($"Timespan {timespan} is invalid. {unit} is not a supported unit.");
        }

        if (unit == "min" && int.Parse(match.Groups[1].Value) < 60)
        {
            throw new ArgumentException($"Timespan {timespan} is invalid. Period must be at least 60 minutes");
        }
    }

    private string FormatDate(string date)
    {
        // If it's already in YYYYMMDDHHMMSS format, return as is
        if (date.Length == 14 && date.All(char.IsDigit))
        {
            return date;
        }

        // If it's in YYYY-MM-DD format, convert to YYYYMMDD000000
        if (DateTime.TryParse(date, out var dateTime))
        {
            return dateTime.ToString("yyyyMMddHHmmss");
        }

        // Try to parse YYYY-MM-DD format
        if (date.Contains('-') && date.Length == 10)
        {
            return date.Replace("-", "") + "000000";
        }

        throw new ArgumentException($"Unsupported date format: {date}");
    }

    private Dictionary<string, JsonElement> LoadJson(string jsonMessage, int maxRecursionDepth = 100, int recursionDepth = 0)
    {
        try
        {
            var jsonDoc = JsonDocument.Parse(jsonMessage);
            if (jsonDoc.RootElement.ValueKind == JsonValueKind.Object)
            {
                return jsonDoc.RootElement.EnumerateObject().ToDictionary(p => p.Name, p => p.Value);
            }
            return new Dictionary<string, JsonElement>();
        }
        catch (JsonException ex)
        {
            if (recursionDepth >= maxRecursionDepth)
            {
                throw new InvalidOperationException("Max recursion depth is reached.", ex);
            }

            // Try to find and remove offending character
            var match = Regex.Match(ex.Message, @"position (\d+)");
            if (match.Success && int.TryParse(match.Groups[1].Value, out var position) && position < jsonMessage.Length)
            {
                var cleaned = jsonMessage.Substring(0, position) + " " + jsonMessage.Substring(position + 1);
                return LoadJson(cleaned, maxRecursionDepth, recursionDepth + 1);
            }

            throw;
        }
    }
}
