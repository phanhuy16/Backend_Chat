using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HtmlAgilityPack;
using Core.DTOs;
using System.Net.Http;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class MetadataController : ControllerBase
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<MetadataController> _logger;

        public MetadataController(IHttpClientFactory httpClientFactory, ILogger<MetadataController> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        [HttpGet("preview")]
        public async Task<IActionResult> GetLinkPreview([FromQuery] string url)
        {
            if (string.IsNullOrEmpty(url))
                return BadRequest("URL is required");

            try
            {
                if (!url.StartsWith("http"))
                    url = "http://" + url;

                _logger.LogInformation("Attempting to fetch metadata for: {Url}", url);

                var client = _httpClientFactory.CreateClient();
                client.Timeout = TimeSpan.FromSeconds(10);
                client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/119.0.0.0 Safari/537.36");
                client.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8");
                
                using var response = await client.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Failed to fetch URL: {Url}. Status: {StatusCode}", url, response.StatusCode);
                    return BadRequest($"Could not fetch URL. Status: {response.StatusCode}");
                }

                var html = await response.Content.ReadAsStringAsync();
                var doc = new HtmlDocument();
                doc.LoadHtml(html);

                var preview = new LinkPreviewDto
                {
                    Url = url,
                    Title = (GetMetaTag(doc, "og:title") ?? GetTagContent(doc, "title") ?? "").Trim(),
                    Description = (GetMetaTag(doc, "og:description") ?? GetMetaTag(doc, "description") ?? "").Trim(),
                    ImageUrl = GetMetaTag(doc, "og:image") ?? "",
                    SiteName = GetMetaTag(doc, "og:site_name") ?? new Uri(url).Host
                };

                return Ok(preview);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP Request error for {Url}", url);
                return BadRequest($"Network error while fetching preview: {ex.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error fetching metadata for {Url}", url);
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        private string? GetMetaTag(HtmlDocument doc, string property)
        {
            var node = doc.DocumentNode.SelectSingleNode($"//meta[@property='{property}']") 
                       ?? doc.DocumentNode.SelectSingleNode($"//meta[@name='{property}']");
            return node?.GetAttributeValue("content", string.Empty);
        }

        private string? GetTagContent(HtmlDocument doc, string tag)
        {
            var node = doc.DocumentNode.SelectSingleNode($"//{tag}");
            return node?.InnerText;
        }
    }
}
