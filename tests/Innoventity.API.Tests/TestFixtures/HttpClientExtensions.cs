using System.Net.Http.Headers;

namespace Innoventity.API.Tests.TestFixtures;

/// <summary>
/// Extension methods for HttpClient to handle authenticated requests in tests.
/// These extensions attach JWT tokens on a per-request basis to ensure proper authentication handling.
/// </summary>
public static class HttpClientExtensions
{
    /// <summary>
    /// Sends a GET request with JWT authentication.
    /// </summary>
    /// <param name="client">The HttpClient instance.</param>
    /// <param name="requestUri">The request URI.</param>
    /// <param name="token">The JWT access token.</param>
    /// <returns>The HTTP response message.</returns>
    public static async Task<HttpResponseMessage> GetWithAuthAsync(
        this HttpClient client,
        string requestUri,
        string token)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await client.SendAsync(request);
    }

    /// <summary>
    /// Sends a POST request with JWT authentication.
    /// </summary>
    /// <param name="client">The HttpClient instance.</param>
    /// <param name="requestUri">The request URI.</param>
    /// <param name="content">The HTTP content to send.</param>
    /// <param name="token">The JWT access token.</param>
    /// <returns>The HTTP response message.</returns>
    public static async Task<HttpResponseMessage> PostWithAuthAsync(
        this HttpClient client,
        string requestUri,
        HttpContent content,
        string token)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, requestUri)
        {
            Content = content
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await client.SendAsync(request);
    }

    /// <summary>
    /// Sends a PUT request with JWT authentication.
    /// </summary>
    /// <param name="client">The HttpClient instance.</param>
    /// <param name="requestUri">The request URI.</param>
    /// <param name="content">The HTTP content to send.</param>
    /// <param name="token">The JWT access token.</param>
    /// <returns>The HTTP response message.</returns>
    public static async Task<HttpResponseMessage> PutWithAuthAsync(
        this HttpClient client,
        string requestUri,
        HttpContent content,
        string token)
    {
        var request = new HttpRequestMessage(HttpMethod.Put, requestUri)
        {
            Content = content
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await client.SendAsync(request);
    }

    /// <summary>
    /// Sends a DELETE request with JWT authentication.
    /// </summary>
    /// <param name="client">The HttpClient instance.</param>
    /// <param name="requestUri">The request URI.</param>
    /// <param name="token">The JWT access token.</param>
    /// <returns>The HTTP response message.</returns>
    public static async Task<HttpResponseMessage> DeleteWithAuthAsync(
        this HttpClient client,
        string requestUri,
        string token)
    {
        var request = new HttpRequestMessage(HttpMethod.Delete, requestUri);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await client.SendAsync(request);
    }

    /// <summary>
    /// Sends a PATCH request with JWT authentication.
    /// </summary>
    /// <param name="client">The HttpClient instance.</param>
    /// <param name="requestUri">The request URI.</param>
    /// <param name="content">The HTTP content to send.</param>
    /// <param name="token">The JWT access token.</param>
    /// <returns>The HTTP response message.</returns>
    public static async Task<HttpResponseMessage> PatchWithAuthAsync(
        this HttpClient client,
        string requestUri,
        HttpContent content,
        string token)
    {
        var request = new HttpRequestMessage(HttpMethod.Patch, requestUri)
        {
            Content = content
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await client.SendAsync(request);
    }
}
