using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace D2Sharp.Tests;

public class RenderApiIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public RenderApiIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task RenderEndpoint_WithValidScript_ReturnsSvg()
    {
        // Arrange
        var client = _factory.CreateClient();
        var request = new { Script = "A -> B" };

        // Act
        var response = await client.PostAsJsonAsync("/render", request);

        // Assert
        response.EnsureSuccessStatusCode();
        Assert.Equal("image/svg+xml", response.Content.Headers.ContentType?.MediaType);

        var svg = await response.Content.ReadAsStringAsync();
        Assert.Contains("<svg", svg);
        Assert.Contains("</svg>", svg);
    }

    [Fact]
    public async Task RenderEndpoint_WithComplexScript_ReturnsSvg()
    {
        // Arrange
        var client = _factory.CreateClient();
        var script = @"
direction: right
A -> B -> C
D -> E -> F
A -> E
";
        var request = new { Script = script };

        // Act
        var response = await client.PostAsJsonAsync("/render", request);

        // Assert
        response.EnsureSuccessStatusCode();
        var svg = await response.Content.ReadAsStringAsync();
        Assert.Contains("<svg", svg);
    }

    [Fact]
    public async Task RenderEndpoint_WithEmptyScript_ReturnsBadRequest()
    {
        // Arrange
        var client = _factory.CreateClient();
        var request = new { Script = "" };

        // Act
        var response = await client.PostAsJsonAsync("/render", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task RenderEndpoint_WithNullScript_ReturnsBadRequest()
    {
        // Arrange
        var client = _factory.CreateClient();
        var content = new StringContent("{\"script\": null}", Encoding.UTF8, "application/json");

        // Act
        var response = await client.PostAsync("/render", content);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task RenderEndpoint_WithInvalidScript_ReturnsErrorDetails()
    {
        // Arrange
        var client = _factory.CreateClient();
        var request = new { Script = "A -> " }; // Incomplete connection

        // Act
        var response = await client.PostAsJsonAsync("/render", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var errorJson = await response.Content.ReadAsStringAsync();
        Assert.Contains("message", errorJson);
    }

    [Fact]
    public async Task RenderEndpoint_WithVeryLongScript_ExceedsMaxLength()
    {
        // Arrange
        var client = _factory.CreateClient();
        var longScript = new string('A', 150000); // Exceeds default limit of 100000
        var request = new { Script = longScript };

        // Act
        var response = await client.PostAsJsonAsync("/render", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var errorMessage = await response.Content.ReadAsStringAsync();
        Assert.Contains("maximum length", errorMessage);
    }

    [Fact]
    public async Task HealthEndpoint_ReturnsHealthy()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/health");

        // Assert
        response.EnsureSuccessStatusCode();
        Assert.Equal("text/plain", response.Content.Headers.ContentType?.MediaType);

        var content = await response.Content.ReadAsStringAsync();
        Assert.Equal("Healthy", content);
    }

    [Fact]
    public async Task HealthReadyEndpoint_ReturnsHealthy()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/health/ready");

        // Assert
        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task HealthLiveEndpoint_ReturnsHealthy()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/health/live");

        // Assert
        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task SwaggerEndpoint_ReturnsSwaggerJson()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/swagger/v1/swagger.json");

        // Assert
        response.EnsureSuccessStatusCode();
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);

        var swaggerJson = await response.Content.ReadAsStringAsync();
        Assert.Contains("openapi", swaggerJson);
        Assert.Contains("D2Sharp API", swaggerJson);
    }

    [Fact]
    public async Task RenderEndpoint_ReturnsCorrectSecurityHeaders()
    {
        // Arrange
        var client = _factory.CreateClient();
        var request = new { Script = "A -> B" };

        // Act
        var response = await client.PostAsJsonAsync("/render", request);

        // Assert
        Assert.True(response.Headers.Contains("X-Content-Type-Options"));
        Assert.True(response.Headers.Contains("X-Frame-Options"));
        Assert.True(response.Headers.Contains("X-XSS-Protection"));
        Assert.True(response.Headers.Contains("Referrer-Policy"));
    }

    [Fact]
    public async Task RenderEndpoint_MultipleRequests_AllSucceed()
    {
        // Arrange
        var client = _factory.CreateClient();
        var request = new { Script = "A -> B -> C" };
        var tasks = new List<Task<HttpResponseMessage>>();

        // Act - Send 5 concurrent requests
        for (int i = 0; i < 5; i++)
        {
            tasks.Add(client.PostAsJsonAsync("/render", request));
        }

        var responses = await Task.WhenAll(tasks);

        // Assert
        foreach (var response in responses)
        {
            response.EnsureSuccessStatusCode();
            var svg = await response.Content.ReadAsStringAsync();
            Assert.Contains("<svg", svg);
        }
    }
}
