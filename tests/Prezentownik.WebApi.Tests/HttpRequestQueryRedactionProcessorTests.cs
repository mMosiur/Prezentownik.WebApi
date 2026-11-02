using System.Diagnostics;
using Prezentownik.WebApi.Startup;
using Xunit;

namespace Prezentownik.WebApi.Tests;

public class HttpRequestQueryRedactionProcessorTests : IDisposable
{
    private readonly ActivitySource _activitySource = new($"query-redaction-tests-{Guid.NewGuid()}");
    private readonly ActivityListener _activityListener;

    public HttpRequestQueryRedactionProcessorTests()
    {
        _activityListener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == _activitySource.Name,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded
        };
        ActivitySource.AddActivityListener(_activityListener);
    }

    [Fact]
    public void OnEnd_ServerSpan_RemovesQueriesFromModernAndLegacyUrlAttributes()
    {
        using var activity = _activitySource.StartActivity("incoming request", ActivityKind.Server)!;
        activity.SetTag("url.full", "https://example.test/lists?revocationToken=secret");
        activity.SetTag("url.query", "revocationToken=secret");
        activity.SetTag("http.url", "https://example.test/legacy?revocationToken=secret");
        activity.SetTag("http.target", "/legacy?revocationToken=secret");

        activity.Stop();
        new HttpRequestQueryRedactionProcessor().OnEnd(activity);

        Assert.Equal("https://example.test/lists", activity.GetTagItem("url.full"));
        Assert.Null(activity.GetTagItem("url.query"));
        Assert.Equal("https://example.test/legacy", activity.GetTagItem("http.url"));
        Assert.Equal("/legacy", activity.GetTagItem("http.target"));
    }

    [Fact]
    public void OnEnd_ServerSpan_PreservesQueryFreeUrls()
    {
        using var activity = _activitySource.StartActivity("incoming request", ActivityKind.Server)!;
        activity.SetTag("url.full", "https://example.test/lists");
        activity.SetTag("http.url", "https://example.test/legacy");
        activity.SetTag("http.target", "/legacy");

        activity.Stop();
        new HttpRequestQueryRedactionProcessor().OnEnd(activity);

        Assert.Equal("https://example.test/lists", activity.GetTagItem("url.full"));
        Assert.Equal("https://example.test/legacy", activity.GetTagItem("http.url"));
        Assert.Equal("/legacy", activity.GetTagItem("http.target"));
    }

    [Fact]
    public void OnEnd_ClientSpan_DoesNotChangeAttributes()
    {
        using var activity = _activitySource.StartActivity("outgoing request", ActivityKind.Client)!;
        activity.SetTag("url.full", "https://example.test/lists?value=kept");
        activity.SetTag("url.query", "value=kept");

        activity.Stop();
        new HttpRequestQueryRedactionProcessor().OnEnd(activity);

        Assert.Equal("https://example.test/lists?value=kept", activity.GetTagItem("url.full"));
        Assert.Equal("value=kept", activity.GetTagItem("url.query"));
    }

    public void Dispose()
    {
        _activityListener.Dispose();
        _activitySource.Dispose();
    }
}