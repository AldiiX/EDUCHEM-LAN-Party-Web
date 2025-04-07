using MimeKit;
using MailKit.Net.Smtp;

namespace EduchemLP.Server.Services;





public static class EmailService {


    public static async Task SendPlainTextEmailAsync(string to, string subject, string body) {
        try {

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(name: "EDUCHEM LAN Party", address: Program.ENV["SMTP_EMAIL_USERNAME"]));
            message.To.Add(new MailboxAddress(name: to, address: to));
            message.Subject = subject;

            message.Body = new TextPart("plain") {
                Text = body
            };

            using var client = new SmtpClient();
            await client.ConnectAsync(host: Program.ENV["SMTP_HOST"], port: int.Parse(Program.ENV["SMTP_PORT"]), useSsl:true);
            await client.AuthenticateAsync(userName: Program.ENV["SMTP_EMAIL_USERNAME"], password: Program.ENV["SMTP_EMAIL_PASSWORD"]);
            await client.SendAsync(message);
            await client.DisconnectAsync(quit: true);
        } catch (Exception ex) {
            Program.Logger.LogError(ex, "Error sending plain text email");
        }
    }

    public static async Task SendHTMLEmailAsync<TModel>(string to, string subject, string razorViewName, TModel model, IServiceProvider? serviceProvider = null, string? fallbackBody = null) {
        serviceProvider ??= HttpContextService.Current.RequestServices;

        try {
            var body = await RazorEngineService.RenderViewToStringAsync(serviceProvider, razorViewName, model);

            // logovaní obsahu
            // Console.WriteLine("Generated email body: ");
            // Console.WriteLine(body);



            // odeslání emailu
            var message = new MimeMessage();
            const string name = "EDUCHEM LAN Party";
            message.From.Add(new MailboxAddress(name: name, address: Program.ENV["SMTP_EMAIL_USERNAME"]));
            message.To.Add(new MailboxAddress(name: to, address: to));
            message.Subject = subject;
            message.Date = DateTimeOffset.Now;
            message.Headers.Add("MIME-Version", "1.0");
            message.Headers.Add("Reply-To", Program.ENV["SMTP_EMAIL_USERNAME"]);
            message.Headers.Add("Content-Type", "text/html; charset=UTF-8");
            //message.Headers.Add("X-Mailer", name);
            message.Headers.Add("Return-Path", Program.ENV["SMTP_EMAIL_USERNAME"]);
            //message.Headers.Add("List-Unsubscribe", $"<mailto:unsubscribe@stanislavskudrna.cz>, <https://{Program.ROOT_DOMAIN}/unsubscribe>");

            var bodyBuilder = new BodyBuilder { HtmlBody = body };
            if(fallbackBody != null) bodyBuilder.TextBody = fallbackBody;

            message.Body = bodyBuilder.ToMessageBody();



            using var client = new SmtpClient();
            await client.ConnectAsync(host: Program.ENV["SMTP_HOST"], port: int.Parse(Program.ENV["SMTP_PORT"]), useSsl: true);
            await client.AuthenticateAsync(userName: Program.ENV["SMTP_EMAIL_USERNAME"], password: Program.ENV["SMTP_EMAIL_PASSWORD"]);
            await client.SendAsync(message);
            await client.DisconnectAsync(quit: true);

            Program.Logger.LogDebug("Email sent successfully.");
        } catch (Exception ex) {
            Program.Logger.LogError(ex, "Error sending HTML email");
        }
    }
}