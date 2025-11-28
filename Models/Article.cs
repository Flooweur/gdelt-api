namespace GdeltApi.Models;

public class Article
{
    public string? Url { get; set; }
    public string? UrlMobile { get; set; }
    public string? Title { get; set; }
    public string? Seendate { get; set; }
    public string? Socialimage { get; set; }
    public string? Domain { get; set; }
    public string? Language { get; set; }
    public string? Sourcecountry { get; set; }
}

public class ArticleSearchResponse
{
    public List<Article> Articles { get; set; } = new();
}
