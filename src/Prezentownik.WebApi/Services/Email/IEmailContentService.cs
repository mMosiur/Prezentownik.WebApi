using Microsoft.AspNetCore.Components;
using Prezentownik.WebApi.Modules.Auth.Email;

namespace Prezentownik.WebApi.Services.Email;

public interface IEmailContentService
{
    Task<EmailBody> RenderEmailBody<TComponent>(
        Dictionary<string, object?> parameters,
        string plainText)
        where TComponent : IComponent;
}
