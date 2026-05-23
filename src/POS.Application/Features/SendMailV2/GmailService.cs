using System.Net.Http;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace POS.Application.Features.SendMail
{
    public class GmailService
    {
        private readonly string _apiKey;
        private readonly HttpClient _httpClient;

        public GmailService(IConfiguration config, HttpClient httpClient)
        {
            _apiKey = config["Brevo:ApiKey"] ?? throw new ArgumentNullException("Brevo:ApiKey is missing");
            _httpClient = httpClient;
        }

        public async Task SendEmailAsync(EmailDto email)
        {
            if (string.IsNullOrWhiteSpace(email?.To))
                throw new ArgumentException("Recipient email address is required");

            if (string.IsNullOrWhiteSpace(email.Subject))
                throw new ArgumentException("Email subject is required");

            var payload = new
            {
                sender = new { name = "Sarana System", email = "khonchhay6@gmail.com" },
                to = new[] { new { email = email.To.Trim() } },
                subject = email.Subject,
                textContent = email.Body ?? string.Empty
            };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var request = new HttpRequestMessage(HttpMethod.Post, "https://api.brevo.com/v3/smtp/email");
            request.Headers.Add("api-key", _apiKey);
            request.Content = content;

            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception($"Brevo API error: {error}");
            }
        }
    }
}