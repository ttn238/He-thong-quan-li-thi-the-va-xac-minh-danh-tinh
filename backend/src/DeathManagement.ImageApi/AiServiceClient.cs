using System.Net;
using System.Net.Http.Headers;
using System.Text;

public sealed record AiProxyResponse(HttpStatusCode Status, string ContentType, byte[] Body);

public sealed class AiServiceClient(HttpClient client)
{
    public async Task<AiProxyResponse> AnalyzeAsync(Stream file, string fileName, string? contentType,
        string? linkedRecordId, CancellationToken cancellationToken)
    {
        using var form = new MultipartFormDataContent();
        var content = new StreamContent(file);
        content.Headers.ContentType = MediaTypeHeaderValue.Parse(contentType!);
        form.Add(content, "file", Path.GetFileName(fileName));
        if (!string.IsNullOrWhiteSpace(linkedRecordId))
            form.Add(new StringContent(linkedRecordId), "linked_record_id");
        using var response = await client.PostAsync("v1/results", form, cancellationToken);
        return await ReadAsync(response, cancellationToken);
    }

    public async Task<AiProxyResponse> GetAsync(string path, CancellationToken cancellationToken)
    {
        using var response = await client.GetAsync(path, cancellationToken);
        return await ReadAsync(response, cancellationToken);
    }

    public async Task<AiProxyResponse> ReviewAsync(string path, string json, CancellationToken cancellationToken)
    {
        using var content = new StringContent(json, Encoding.UTF8, "application/json");
        using var response = await client.PostAsync(path, content, cancellationToken);
        return await ReadAsync(response, cancellationToken);
    }

    private static async Task<AiProxyResponse> ReadAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var body = await response.Content.ReadAsByteArrayAsync(cancellationToken);
        var type = response.Content.Headers.ContentType?.ToString() ?? "application/json";
        return new(response.StatusCode, type, body);
    }
}
