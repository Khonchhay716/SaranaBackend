using MimeKit;
using MailKit.Net.Smtp;
using POS.Application.Features.SendMail;
using Microsoft.Extensions.Configuration;

namespace POS.Application.Features.SendMail
{
    public class GmailService
    {
        private readonly string _username;
        private readonly string _appPassword;

        public GmailService(IConfiguration config)
        {
            _username = config["Gmail:Username"] ?? throw new ArgumentNullException("Gmail:Username is missing in configuration");
            _appPassword = config["Gmail:AppPassword"] ?? throw new ArgumentNullException("Gmail:AppPassword is missing in configuration");
        }

        public async Task SendEmailAsync(EmailDto email)
        {
            // Input validation
            if (string.IsNullOrWhiteSpace(email?.To))
                throw new ArgumentException("Recipient email address is required");

            if (string.IsNullOrWhiteSpace(email.Subject))
                throw new ArgumentException("Email subject is required");

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("CoffeeManagementSystem", _username));
            message.To.Add(new MailboxAddress("Recipient", email.To.Trim()));
            message.Subject = email.Subject;
            message.Body = new TextPart("plain") { Text = email.Body ?? string.Empty };

            using var client = new SmtpClient();

            try
            {
                await client.ConnectAsync("smtp.gmail.com", 587, MailKit.Security.SecureSocketOptions.StartTls);
                await client.AuthenticateAsync(_username, _appPassword);
                await client.SendAsync(message);
            }
            finally
            {
                await client.DisconnectAsync(true);
            }
        }
    }
}