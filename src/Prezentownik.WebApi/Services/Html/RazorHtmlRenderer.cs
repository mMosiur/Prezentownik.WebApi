using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Prezentownik.WebApi.Services.Html;

public sealed class RazorHtmlRenderer(
    IServiceProvider serviceProvider,
    ILoggerFactory loggerFactory)
    : IRazorHtmlRenderer
{
    private readonly IServiceProvider _serviceProvider = serviceProvider;
    private readonly ILoggerFactory _loggerFactory = loggerFactory;

    public async Task<string> RenderAsync<TComponent>(Dictionary<string, object?> parameters)
        where TComponent : IComponent
    {
        await using var htmlRenderer = new HtmlRenderer(_serviceProvider, _loggerFactory);
        return await htmlRenderer.Dispatcher.InvokeAsync(async () =>
        {
            var parameterView = ParameterView.FromDictionary(parameters);
            var output = await htmlRenderer.RenderComponentAsync<TComponent>(parameterView);
            return output.ToHtmlString();
        });
    }
}
