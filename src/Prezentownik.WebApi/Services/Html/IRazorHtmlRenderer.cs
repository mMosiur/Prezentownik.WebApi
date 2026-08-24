using Microsoft.AspNetCore.Components;

namespace Prezentownik.WebApi.Services.Html;

public interface IRazorHtmlRenderer
{
    Task<string> RenderAsync<TComponent>(Dictionary<string, object?> parameters) where TComponent : IComponent;
}
