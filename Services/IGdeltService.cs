using GdeltApi.Models;

namespace GdeltApi.Services;

public interface IGdeltService
{
    Task<List<Article>> ArticleSearchAsync(FiltersDto filters);
    Task<List<Dictionary<string, object>>> TimelineSearchAsync(string mode, FiltersDto filters);
    Task<List<Article>> GetLastHourAsync(FiltersDto? filters = null);
}
