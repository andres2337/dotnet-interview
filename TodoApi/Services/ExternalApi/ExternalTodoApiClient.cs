using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using TodoApi.Options;

namespace TodoApi.Services.ExternalApi;

public class ExternalTodoApiClient : IExternalTodoApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ExternalTodoApiOptions _options;
    private readonly ILogger<ExternalTodoApiClient> _logger;

    public ExternalTodoApiClient(HttpClient httpClient, IOptions<ExternalTodoApiOptions> options, ILogger<ExternalTodoApiClient> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
        _httpClient.BaseAddress = new Uri(_options.BaseUrl);
        _httpClient.Timeout = TimeSpan.FromSeconds(_options.TimeoutSeconds);
    }

    public Task<IReadOnlyList<ExternalTodoListDto>> GetTodoListsAsync(CancellationToken cancellationToken)
        => SendWithRetryAsync(async () =>
        {
            var response = await _httpClient.GetAsync("/todolists", cancellationToken);
            await EnsureSuccessAsync(response, "list todo lists", cancellationToken);
            var payload = await response.Content.ReadFromJsonAsync<List<ExternalTodoListDto>>(cancellationToken: cancellationToken);
            return (IReadOnlyList<ExternalTodoListDto>)(payload ?? []);
        }, cancellationToken);

    public Task<ExternalTodoListDto> CreateTodoListAsync(ExternalCreateTodoListRequest request, CancellationToken cancellationToken)
        => SendWithRetryAsync(async () =>
        {
            var response = await _httpClient.PostAsJsonAsync("/todolists", request, cancellationToken);
            await EnsureSuccessAsync(response, "create todo list", cancellationToken);
            var payload = await response.Content.ReadFromJsonAsync<ExternalTodoListDto>(cancellationToken: cancellationToken);
            if (payload is null)
            {
                throw new InvalidOperationException("External API returned an empty create todo list response.");
            }
            return payload;
        }, cancellationToken);

    public Task UpdateTodoListAsync(long externalTodoListId, ExternalUpdateTodoListRequest request, CancellationToken cancellationToken)
        => SendWithRetryAsync(async () =>
        {
            var response = await _httpClient.PatchAsJsonAsync($"/todolists/{externalTodoListId}", request, cancellationToken);
            await EnsureSuccessAsync(response, "update todo list", cancellationToken);
            return true;
        }, cancellationToken);

    public Task DeleteTodoListAsync(long externalTodoListId, CancellationToken cancellationToken)
        => SendWithRetryAsync(async () =>
        {
            var response = await _httpClient.DeleteAsync($"/todolists/{externalTodoListId}", cancellationToken);
            await EnsureSuccessAsync(response, "delete todo list", cancellationToken);
            return true;
        }, cancellationToken);

    public Task UpdateTodoItemAsync(long externalTodoListId, long externalTodoItemId, ExternalUpdateTodoItemRequest request, CancellationToken cancellationToken)
        => SendWithRetryAsync(async () =>
        {
            var response = await _httpClient.PatchAsJsonAsync($"/todolists/{externalTodoListId}/todoitems/{externalTodoItemId}", request, cancellationToken);
            await EnsureSuccessAsync(response, "update todo item", cancellationToken);
            return true;
        }, cancellationToken);

    public Task DeleteTodoItemAsync(long externalTodoListId, long externalTodoItemId, CancellationToken cancellationToken)
        => SendWithRetryAsync(async () =>
        {
            var response = await _httpClient.DeleteAsync($"/todolists/{externalTodoListId}/todoitems/{externalTodoItemId}", cancellationToken);
            await EnsureSuccessAsync(response, "delete todo item", cancellationToken);
            return true;
        }, cancellationToken);

    private async Task<T> SendWithRetryAsync<T>(Func<Task<T>> action, CancellationToken cancellationToken)
    {
        Exception? lastException = null;

        for (var attempt = 0; attempt <= _options.RetryCount; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                return await action();
            }
            catch (Exception ex) when (IsTransient(ex) && attempt < _options.RetryCount)
            {
                lastException = ex;
                var delay = TimeSpan.FromMilliseconds(_options.RetryDelayMs * Math.Pow(2, attempt));
                _logger.LogWarning(ex, "Transient error calling external todo API on attempt {Attempt}. Retrying in {Delay} ms.", attempt + 1, delay.TotalMilliseconds);
                await Task.Delay(delay, cancellationToken);
            }
            catch (Exception ex)
            {
                lastException = ex;
                break;
            }
        }

        throw lastException ?? new InvalidOperationException("External todo API call failed.");
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, string operation, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        throw new HttpRequestException($"External API failed to {operation}. Status: {(int)response.StatusCode}. Body: {body}", null, response.StatusCode);
    }

    private static bool IsTransient(Exception exception)
    {
        if (exception is TaskCanceledException)
        {
            return true;
        }

        if (exception is HttpRequestException httpException)
        {
            return httpException.StatusCode is null or HttpStatusCode.RequestTimeout or HttpStatusCode.TooManyRequests or HttpStatusCode.BadGateway or HttpStatusCode.ServiceUnavailable or HttpStatusCode.GatewayTimeout || ((int?)httpException.StatusCode >= 500);
        }

        return false;
    }
}
