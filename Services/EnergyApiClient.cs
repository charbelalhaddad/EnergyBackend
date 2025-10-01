using System.Net.Http.Headers;
using System.Text.Json;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace EnergyBackend.Services
{
    public record ExternalReading(DateTime timestamp, decimal value);
    internal sealed record TokenResponse(string access_token, string token_type);

    public class EnergyApiClient
    {
        private readonly HttpClient _http;
        private readonly IConfiguration _config;

        private string? _cachedToken;
        private DateTimeOffset _tokenExpiry = DateTimeOffset.MinValue;

        public EnergyApiClient(HttpClient http, IConfiguration config)
        {
            _http = http;
            _config = config;

            var baseUrl = _config["ExternalApi:BaseUrl"] ?? "https://assignment.stellarblue.eu";
            _http.BaseAddress ??= new Uri(baseUrl);
        }

        public async Task<IReadOnlyList<ExternalReading>> GetReadingsAsync(
            DateTime fromUtc,
            DateTime toUtc,
            CancellationToken ct = default)
        {
            var token = await GetTokenAsync(ct);

            using var req = new HttpRequestMessage(
                HttpMethod.Get,
                $"/MCP?date_from={fromUtc:yyyy-MM-dd}&date_to={toUtc:yyyy-MM-dd}");

            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            using var res = await _http.SendAsync(req, ct);

            if (!res.IsSuccessStatusCode)
            {
                var body = await res.Content.ReadAsStringAsync(ct);
                throw new InvalidOperationException($"MCP request failed ({(int)res.StatusCode} {res.ReasonPhrase}). Body: {body}");
            }

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var list = await res.Content.ReadFromJsonAsync<List<ExternalReading>>(options, ct);
            return list ?? new List<ExternalReading>();
        }

        private async Task<string> GetTokenAsync(CancellationToken ct)
        {
            if (!string.IsNullOrWhiteSpace(_cachedToken) && _tokenExpiry > DateTimeOffset.UtcNow.AddMinutes(1))
                return _cachedToken!;

            var username = _config["ExternalApi:Username"] ?? "stellarblue";
            var password = _config["ExternalApi:Password"] ?? "st3!!@r_b1u3";

            var payload = new
            {
                username,
                password
            };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            using var req = new HttpRequestMessage(HttpMethod.Post, "/token")
            {
                Content = content
            };

            using var res = await _http.SendAsync(req, ct);

            if (!res.IsSuccessStatusCode)
            {
                var body = await res.Content.ReadAsStringAsync(ct);
                throw new InvalidOperationException($"Token request failed ({(int)res.StatusCode} {res.ReasonPhrase}). Body: {body}");
            }

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var tokenResponse = await res.Content.ReadFromJsonAsync<TokenResponse>(options, ct);

            if (tokenResponse is null || string.IsNullOrWhiteSpace(tokenResponse.access_token))
                throw new InvalidOperationException("Token response missing or empty.");

            _cachedToken = tokenResponse.access_token;
            _tokenExpiry = DateTimeOffset.UtcNow.AddMinutes(30); // Adjust if API provides expiry

            return _cachedToken!;
        }
    }
}
