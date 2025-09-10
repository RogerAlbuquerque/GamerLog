using System.Net;

namespace GameLog.Tests.Unit.Support;

public sealed record RecordedRequest(HttpMethod Method, string Url, string Body, IReadOnlyDictionary<string, string> Headers);

/// <summary>Scripted HTTP server for provider tests: records every request and answers from a delegate.</summary>
public sealed class FakeHandler(Func<RecordedRequest, HttpResponseMessage> respond) : HttpMessageHandler
{
    public List<RecordedRequest> Requests { get; } = [];

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        var body = request.Content is null ? "" : await request.Content.ReadAsStringAsync(ct);
        var headers = request.Headers.ToDictionary(h => h.Key, h => string.Join(",", h.Value));
        var recorded = new RecordedRequest(request.Method, request.RequestUri!.ToString(), body, headers);
        Requests.Add(recorded);
        return respond(recorded);
    }

    public static HttpResponseMessage Json(string json, HttpStatusCode status = HttpStatusCode.OK) =>
        new(status) { Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json") };
}

public sealed class FakeHttpFactory(HttpMessageHandler handler) : IHttpClientFactory
{
    public HttpClient CreateClient(string name) => new(handler, disposeHandler: false);
}
