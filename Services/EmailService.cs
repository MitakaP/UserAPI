using MailKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using UserAPI.Interfaces;

namespace UserAPI.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;

    public EmailService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task SendEmailAsync(string to, string subject, string body)
    {
        var emailSettings = _configuration.GetSection("EmailSettings");

        var message = new MimeMessage();

        message.From.Add(
            MailboxAddress.Parse(emailSettings["SenderEmail"]!));

        message.To.Add(
            MailboxAddress.Parse(to));

        message.Subject = subject;

        message.Body = new TextPart("plain")
        {
            Text = body
        };

        using var client = new SmtpClient();

        await client.ConnectAsync(
            emailSettings["SmtpServer"],
            int.Parse(emailSettings["Port"]!),
            SecureSocketOptions.StartTls);

        await client.AuthenticateAsync(
            emailSettings["Username"],
            emailSettings["AppPassword"]);

        await client.SendAsync(message);

        await client.DisconnectAsync(true);
    }
}
