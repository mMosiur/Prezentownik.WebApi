using Microsoft.AspNetCore.Components;
using Prezentownik.WebApi.Services.Html;

namespace Prezentownik.WebApi.Services.Email;

public sealed class EmailContentService(IRazorHtmlRenderer razorHtmlRenderer) : IEmailContentService
{
    private readonly IRazorHtmlRenderer _razorHtmlRenderer = razorHtmlRenderer;

    public async Task<EmailBody> RenderEmailBody<TComponent>(
        Dictionary<string, object?> parameters,
        string plainText)
        where TComponent : IComponent
    {
        var html = await _razorHtmlRenderer.RenderAsync<TComponent>(parameters);

        return new(html, plainText);
    }
}
