using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Collections.Concurrent;

namespace Bioteca.Prism.InteroperableResearchNode.Test;

/// <summary>
/// Custom WebApplicationFactory for integration testing
/// </summary>
public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    private string _environmentName = "Development";
    private static readonly ConcurrentDictionary<string, TestWebApplicationFactory> _remoteFactories = new();

    public TestWebApplicationFactory()
    {
    }

    /// <summary>
    /// Create a factory with a specific environment name
    /// </summary>
    public static TestWebApplicationFactory Create(string environmentName)
    {
        var factory = new TestWebApplicationFactory();
        factory._environmentName = environmentName;
        return factory;
    }

    /// <summary>
    /// Register a remote factory that can be accessed by HttpClient
    /// </summary>
    public static void RegisterRemoteFactory(string baseUrl, TestWebApplicationFactory factory)
    {
        _remoteFactories[baseUrl] = factory;
    }

    /// <summary>
    /// Get a remote factory by base URL
    /// </summary>
    public static TestWebApplicationFactory? GetRemoteFactory(string baseUrl)
    {
        _remoteFactories.TryGetValue(baseUrl, out var factory);
        return factory;
    }

    /// <summary>
    /// Clear all remote factory registrations
    /// </summary>
    public static void ClearRemoteFactories()
    {
        _remoteFactories.Clear();
    }

    /// <summary>
    /// Get all registered URLs for debugging
    /// </summary>
    public static IEnumerable<string> GetAllRegisteredUrls()
    {
        return _remoteFactories.Keys;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(_environmentName);

        builder.ConfigureServices(services =>
        {
            // Replace IHttpClientFactory with test implementation
            var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IHttpClientFactory));
            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            services.AddSingleton<IHttpClientFactory, TestHttpClientFactory>();
        });
    }
}

/// <summary>
/// Custom HttpClientFactory for testing that routes requests to in-memory test servers
/// </summary>
public class TestHttpClientFactory : IHttpClientFactory
{
    public HttpClient CreateClient(string name)
    {
        // Return a special HttpClient that can route to test factories
        return new HttpClient(new TestHttpMessageHandler())
        {
            BaseAddress = new Uri("http://localhost")
        };
    }
}

/// <summary>
/// Custom HttpMessageHandler that routes requests to registered TestWebApplicationFactory instances
/// </summary>
public class TestHttpMessageHandler : DelegatingHandler
{
    public TestHttpMessageHandler() : base(new HttpClientHandler())
    {
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.RequestUri == null)
        {
            throw new InvalidOperationException("Request URI is null");
        }

        // Extract base URL (scheme + host + port)
        // Normalize: don't include default ports (80 for HTTP, 443 for HTTPS)
        var port = request.RequestUri.Port;
        var isDefaultPort = (request.RequestUri.Scheme == "http" && port == 80) ||
                            (request.RequestUri.Scheme == "https" && port == 443);

        var baseUrl = isDefaultPort
            ? $"{request.RequestUri.Scheme}://{request.RequestUri.Host}"
            : $"{request.RequestUri.Scheme}://{request.RequestUri.Host}:{port}";

        // Try to find a registered factory for this URL
        var factory = TestWebApplicationFactory.GetRemoteFactory(baseUrl);

        if (factory != null)
        {
            // Route to the in-memory test server
            var client = factory.CreateClient();

            // Create a new request with relative URI for the test client
            using var testRequest = new HttpRequestMessage(request.Method, request.RequestUri.PathAndQuery);

            // Copy headers
            foreach (var header in request.Headers)
            {
                testRequest.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }

            // Copy content if present
            if (request.Content != null)
            {
                // Read content as stream to avoid multiple reads
                var ms = new MemoryStream();
                await request.Content.CopyToAsync(ms, cancellationToken);
                ms.Position = 0;

                testRequest.Content = new StreamContent(ms);

                // Copy content headers
                foreach (var header in request.Content.Headers)
                {
                    testRequest.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }
            }

            return await client.SendAsync(testRequest, cancellationToken);
        }

        // If no factory found, make a real HTTP request (for external URLs)
        return await base.SendAsync(request, cancellationToken);
    }
}
