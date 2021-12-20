using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace DotnetOutboxPattern.Tests
{
    /// <summary>
    /// Extension methods for <see cref="IntegrationTestFixture"/>.
    /// </summary>
    public static class IntegrationTestFixtureExtensions
    {
        /// <summary>
        /// Sends a GET request to the specified <paramref name="relativeUrl"/> using the fixture's
        /// <see cref="IntegrationTestFixture.Client"/> and deserializes the JSON response to <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The type to deserialize the response body to.</typeparam>
        /// <param name="fixture">The test fixture.</param>
        /// <param name="relativeUrl">The relative URL to request.</param>
        /// <returns>A task that resolves to the deserialized response.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="fixture"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="relativeUrl"/> is null or empty.</exception>
        /// <exception cref="HttpRequestException">Thrown when the HTTP response is unsuccessful.</exception>
        /// <exception cref="JsonException">Thrown when the response cannot be deserialized to <typeparamref name="T"/>.</exception>
        public static async Task<T> GetJsonAsync<T>(this IntegrationTestFixture fixture, string relativeUrl)
        {
            ArgumentNullException.ThrowIfNull(fixture);
            ArgumentException.ThrowIfNullOrEmpty(relativeUrl);

            var response = await fixture.Client.GetAsync(relativeUrl).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            return JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                ?? throw new JsonException($"Unable to deserialize response to {typeof(T)}.");
        }

        /// <summary>
        /// Sends a POST request with a JSON payload to <paramref name="relativeUrl"/> and deserializes the JSON response to <typeparamref name="TResponse"/>.
        /// </summary>
        /// <typeparam name="TRequest">The type of the request payload.</typeparam>
        /// <typeparam name="TResponse">The type of the response payload.</typeparam>
        /// <param name="fixture">The test fixture.</param>
        /// <param name="relativeUrl">The relative URL to post to.</param>
        /// <param name="payload">The request payload.</param>
        /// <returns>A task that resolves to the deserialized response.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="fixture"/> or <paramref name="payload"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="relativeUrl"/> is null or empty.</exception>
        /// <exception cref="HttpRequestException">Thrown when the HTTP response is unsuccessful.</exception>
        /// <exception cref="JsonException">Thrown when the response cannot be deserialized to <typeparamref name="TResponse"/>.</exception>
        public static async Task<TResponse> PostJsonAsync<TRequest, TResponse>(this IntegrationTestFixture fixture, string relativeUrl, TRequest payload)
        {
            ArgumentNullException.ThrowIfNull(fixture);
            ArgumentException.ThrowIfNullOrEmpty(relativeUrl);
            ArgumentNullException.ThrowIfNull(payload);

            var json = JsonSerializer.Serialize(payload);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await fixture.Client.PostAsync(relativeUrl, content).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            return JsonSerializer.Deserialize<TResponse>(responseJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                ?? throw new JsonException($"Unable to deserialize response to {typeof(TResponse)}.");
        }

        /// <summary>
        /// Executes an <paramref name="action"/> with a scoped <see cref="IServiceProvider"/> obtained from the fixture.
        /// The created scope is disposed after the action completes.
        /// </summary>
        /// <param name="fixture">The test fixture.</param>
        /// <param name="action">The action to execute.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="fixture"/> or <paramref name="action"/> is null.</exception>
        public static void WithScope(this IntegrationTestFixture fixture, Action<IServiceProvider> action)
        {
            ArgumentNullException.ThrowIfNull(fixture);
            ArgumentNullException.ThrowIfNull(action);

            using var scope = fixture.CreateScope();
            action(scope.ServiceProvider);
        }

        /// <summary>
        /// Retrieves a service of type <typeparamref name="T"/> from a newly created scope.
        /// The scope is disposed after the service is resolved.
        /// </summary>
        /// <typeparam name="T">The type of service to resolve.</typeparam>
        /// <param name="fixture">The test fixture.</param>
        /// <returns>The resolved service.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="fixture"/> is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the service cannot be resolved.</exception>
        public static T GetScopedService<T>(this IntegrationTestFixture fixture) where T : notnull
        {
            ArgumentNullException.ThrowIfNull(fixture);

            using var scope = fixture.CreateScope();
            var service = scope.ServiceProvider.GetService<T>();
            return service ?? throw new InvalidOperationException($"Service of type {typeof(T)} is not registered.");
        }
    }
}
