# GDELT API - C# Web API

A C# ASP.NET Core Web API for querying the GDELT 2.0 Doc API. This project provides REST endpoints to search for articles and retrieve timeline data from GDELT.

## Features

- **Article Search**: Search for news articles matching specific filters
- **Timeline Search**: Get timeline data in various modes (volume, tone, language, source country)
- **Get Last Hour**: Retrieve articles from the last hour with optional filters

## API Endpoints

### POST /api/gdelt/article_search

Search for articles matching the provided filters.

**Request Body:**
```json
{
  "keyword": ["climate change"],
  "startDate": "2024-01-01",
  "endDate": "2024-01-02",
  "numRecords": 250,
  "domain": ["bbc.co.uk"],
  "country": ["US", "UK"],
  "language": ["eng"]
}
```

**Response:**
```json
{
  "articles": [
    {
      "url": "https://...",
      "urlMobile": "https://...",
      "title": "Article Title",
      "seendate": "20240101120000",
      "socialimage": "https://...",
      "domain": "bbc.co.uk",
      "language": "eng",
      "sourcecountry": "UK"
    }
  ]
}
```

### POST /api/gdelt/timeline_search?mode=timelinevol

Get a timeline of news coverage matching the filters.

**Query Parameters:**
- `mode`: One of `timelinevol`, `timelinevolraw`, `timelinetone`, `timelinelang`, `timelinesourcecountry`

**Request Body:**
```json
{
  "keyword": ["climate change"],
  "startDate": "2024-01-01",
  "endDate": "2024-01-02"
}
```

**Response:**
```json
{
  "data": [
    {
      "datetime": "20240101120000",
      "All Articles": 1234.5
    }
  ]
}
```

### POST /api/gdelt/get_last_hour

Get articles from the last hour, optionally filtered.

**Request Body (optional):**
```json
{
  "keyword": ["climate change"],
  "domain": ["bbc.co.uk"]
}
```

**Response:**
Same format as `article_search`

## Filter Parameters

- `startDate` / `endDate`: Date range in YYYY-MM-DD format (or provide `timespan` instead)
- `timespan`: Relative time span (e.g., "1h", "24h", "7d", "30days")
- `numRecords`: Number of records to return (max 250, default 250)
- `keyword`: Array of keywords to search for
- `domain`: Array of domains to filter by
- `domainExact`: Array of exact domain matches
- `country`: Array of FIPS 2-letter country codes
- `language`: Array of ISO 639 language codes
- `theme`: Array of GDELT GKG themes
- `tone`: Tone filter (e.g., ">5", "<-5")
- `toneAbsolute`: Absolute tone filter
- `near`: Near filter string (use helper functions to construct)
- `repeat`: Repeat filter string (use helper functions to construct)

## Running with Docker

### Build and run with Docker Compose

```bash
docker-compose up --build
```

The API will be available at `http://localhost:8080`

### Build and run with Docker

```bash
docker build -t gdelt-api .
docker run -p 8080:8080 gdelt-api
```

## Running Locally

### Prerequisites

- .NET 8.0 SDK

### Run

```bash
dotnet restore
dotnet run
```

The API will be available at `https://localhost:5001` or `http://localhost:5000`

### Swagger UI

When running in Development mode, Swagger UI is available at `/swagger`

## Development

This project uses:
- ASP.NET Core 8.0
- System.Text.Json for JSON serialization
- HttpClient for API calls

## License

See LICENSE file for details.
