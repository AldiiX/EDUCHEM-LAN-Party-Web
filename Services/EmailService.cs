using MimeKit;
using MailKit.Net.Smtp;

namespace EduchemLPR.Services;





public static class EmailService {


    public static async Task SendPrimitiveEmailAsync(string to, string subject, string body) {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(name: "EDUCHEM LAN Party", address: Program.ENV["SMTP_EMAIL_USERNAME"]));
        message.To.Add(new MailboxAddress(name: to, address: to));
        message.Subject = subject;

        message.Body = new TextPart("plain") {
            Text = body
        };

        using var client = new SmtpClient();
        await client.ConnectAsync(host: Program.ENV["SMTP_HOST"], port: int.Parse(Program.ENV["SMTP_PORT"]), useSsl: true);
        await client.AuthenticateAsync(userName: Program.ENV["SMTP_EMAIL_USERNAME"], password: Program.ENV["SMTP_EMAIL_PASSWORD"]);
        await client.SendAsync(message);
        await client.DisconnectAsync(quit: true);
    }

    public static async Task SendHTMLEmailAsync<TModel>(string to, string subject, string razorViewName, TModel model, IServiceProvider? serviceProvider = null) {
        serviceProvider ??= HttpContextService.Current.RequestServices;

        try {
            var body = await RazorEngineService.RenderViewToStringAsync(serviceProvider, razorViewName, model);

            // logovaní obsahu
            // Console.WriteLine("Generated email body: ");
            // Console.WriteLine(body);



            // odeslání emailu
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(name: "EDUCHEM LAN Party", address: Program.ENV["SMTP_EMAIL_USERNAME"]));
            message.To.Add(new MailboxAddress(name: to, address: to));
            message.Subject = subject;

            message.Body = new TextPart("html") { Text = body };

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