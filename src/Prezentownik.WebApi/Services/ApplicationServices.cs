using System.Text.Encodings.Web;
using System.Text.Unicode;
using Prezentownik.WebApi.Services.Email;
using Prezentownik.WebApi.Services.Html;

namespace Prezentownik.WebApi.Services;

public static class ApplicationServices
{
    public static IHostApplicationBuilder AddApplicationServices(this IHostApplicationBuilder builder)
    {
        builder.Services.AddSingleton(HtmlEncoder.Create(UnicodeRanges.All));
        builder.Services.AddTransient<IRazorHtmlRenderer, RazorHtmlRenderer>();
        builder.Services.AddTransient<IEmailContentService, EmailContentService>();

        if(builder.Environment.IsDevelopment())
            builder.Services.AddTransient<IEmailService, LoggingOnlyEmailService>();
        else
            builder.Services.AddTransient<IEmailService, EmailService>();

        return builder;
    }
}
