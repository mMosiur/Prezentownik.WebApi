using Prezentownik.WebApi.Services.Email;

namespace Prezentownik.WebApi.Services;

public static class ApplicationServices
{
    public static IHostApplicationBuilder AddApplicationServices(this IHostApplicationBuilder builder)
    {
        if(builder.Environment.IsDevelopment())
            builder.Services.AddTransient<IEmailService, LoggingOnlyEmailService>();
        else
            builder.Services.AddTransient<IEmailService, EmailService>();

        return builder;
    }
}
