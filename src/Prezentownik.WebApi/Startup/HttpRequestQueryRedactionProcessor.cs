using System.Diagnostics;
using OpenTelemetry;

namespace Prezentownik.WebApi.Startup;

/// <summary>
/// Removes query strings from incoming HTTP trace attributes before export, including legacy attributes,
/// so cancellation tokens passed in query parameters are not exposed in telemetry.
/// </summary>
internal sealed class HttpRequestQueryRedactionProcessor : BaseProcessor<Activity>
{
    public override void OnEnd(Activity activity)
    {
        if (activity.Kind != ActivityKind.Server) return;

        foreach (var tag in activity.TagObjects)
        {
            switch (tag.Key)
            {
                case "url.query" or "http.query":
                    RedactQuery(activity, tag);
                    break;
                case "url.full" or "http.url" or "http.target" when tag.Value is string url:
                    RedactQueryParameters(activity, url, tag);
                    break;
            }
        }
    }

    private static void RedactQuery(Activity activity, KeyValuePair<string, object?> tag)
    {
        activity.SetTag(tag.Key, null);
    }

    private static void RedactQueryParameters(Activity activity, string url, KeyValuePair<string, object?> tag)
    {
        var queryStart = url.IndexOf('?');
        if (queryStart >= 0)
        {
            activity.SetTag(tag.Key, url[..queryStart]);
        }
    }
}
