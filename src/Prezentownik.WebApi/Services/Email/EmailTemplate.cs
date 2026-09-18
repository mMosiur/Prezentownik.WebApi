namespace Prezentownik.WebApi.Services.Email;

internal static class EmailTemplate
{
    public static string RenderEmailHtml(
        string title,
        string heading,
        string buttonText,
        string buttonUrl,
        string footerNote,
        string childContent)
    {
        //language=html
        return $"""
               <!DOCTYPE html>
               <html lang="pl">
               <head>
                   <meta charset="utf-8">
                   <meta name="viewport" content="width=device-width, initial-scale=1.0">
                   <title>{title}</title>
               </head>
               <body
                   style="margin: 0; padding: 0; background-color: #F4F5F7; font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif;">
               <table role="presentation" border="0" cellpadding="0" cellspacing="0" width="100%"
                      style="background-color: #F4F5F7; padding: 32px 16px;">
                   <tr>
                       <td align="center">
                           <table role="presentation" border="0" cellpadding="0" cellspacing="0" width="100%"
                                  style="max-width: 560px; background-color: #FFFFFF; border-radius: 12px; box-shadow: 0 1px 3px rgba(0,0,0,0.08); overflow: hidden;">
                               <!-- Header -->
                               <tr>
                                   <td style="padding: 28px 32px 20px; background-color: #FFFFFF; border-bottom: 1px solid #F3F4F6;">
                                       <table role="presentation" border="0" cellpadding="0" cellspacing="0">
                                           <tr>
                                               <td style="vertical-align: middle; padding-right: 12px;">
                                                   <img src="https://prezentownik.info.pl/prezentownik-icon.png"
                                                        alt="Prezentownik Logo"
                                                        width="36"
                                                        height="36"
                                                        style="display: block; width: 36px; height: 36px; border-radius: 8px; border: 0;"/>
                                               </td>
                                               <td style="vertical-align: middle;">
                                                       <span
                                                           style="font-size: 22px; font-weight: 700; color: #111827; font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif; line-height: 36px;">
                                                           Prezentownik
                                                       </span>
                                               </td>
                                           </tr>
                                       </table>
                                   </td>
                               </tr>
                               <!-- Main Content -->
                               <tr>
                                   <td style="padding: 32px;">
                                       <h2 style="margin: 0 0 16px; font-size: 20px; font-weight: 600; color: #111827;">
                                           {heading}
                                       </h2>
                                     {childContent}
                                     {RenderButton(buttonText, buttonUrl)}
                                   </td>
                               </tr>
                               <!-- Footer -->
                               <tr>
                                   <td style="padding: 24px 32px; background-color: #FAFAFA; border-top: 1px solid #F3F4F6; text-align: center;">
                                   {RenderFooter(footerNote)}
                                   <p style="margin: 0; font-size: 12px; color: #9CA3AF;">
                                       © {DateTime.UtcNow.Year} Prezentownik.info.pl
                                   </p>
                                   </td>
                               </tr>
                           </table>
                       </td>
                   </tr>
               </table>
               </body>
               </html>
               """;
    }

    private static string RenderButton(string buttonText, string buttonUrl)
    {
        if (string.IsNullOrEmpty(buttonText) || string.IsNullOrEmpty(buttonUrl))
            return "";

        //language=html
        return $"""
               <table role="presentation" border="0" cellpadding="0" cellspacing="0"
                      style="margin: 28px auto;">
                   <tr>
                       <td align="center" style="border-radius: 8px; background-color: #2563EB;">
                           <a href="{buttonUrl}" target="_blank"
                              style="display: inline-block; padding: 14px 28px; font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif; font-size: 16px; font-weight: 600; color: #FFFFFF; text-decoration: none; border-radius: 8px; background-color: #2563EB;">
                               {buttonText}
                           </a>
                       </td>
                   </tr>
               </table>
               <p style="margin: 20px 0 0; font-size: 13px; line-height: 18px; color: #6B7280; word-break: break-all;">
                   Jeśli przycisk nie działa, skopiuj i wklej ten link do przeglądarki:<br/>
                   <a href="{buttonUrl}" style="color: #2563EB; text-decoration: underline;">{buttonUrl}</a>
               </p>
               """;
    }

    private static string RenderFooter(string footerNote)
    {
        if (string.IsNullOrEmpty(footerNote))
            return "";

        //language=html
        return $"""
                <p style="margin: 0 0 12px; font-size: 12px; line-height: 18px; color: #9CA3AF;">{footerNote}</p>
                """;
    }
}
