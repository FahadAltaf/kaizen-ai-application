using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Kaizen.Entities
{
    public class WhatsAppService
    {
        private readonly IHttpClientFactory _clientFactory;
        private readonly ILogger<WhatsAppService> _logger;
        private static readonly string _url = "https://graph.facebook.com/v18.0/193557653834811/messages";
        private static readonly string _token = Environment.GetEnvironmentVariable("MetaKey");

        public WhatsAppService(IHttpClientFactory clientFactory, ILogger<WhatsAppService> logger)
        {
            _clientFactory = clientFactory;
            _logger = logger;
        }
        static string RemoveSourceReference(string text)
        {
            string startPattern = "【";
            string endPattern = "】";
            int startIndex = text.IndexOf(startPattern);
            int endIndex;

            while (startIndex != -1)
            {
                endIndex = text.IndexOf(endPattern, startIndex);
                if (endIndex != -1)
                {
                    // Remove the text from startPattern to endPattern
                    text = text.Remove(startIndex, endIndex - startIndex + endPattern.Length);
                }
                else
                {
                    // If there's no endPattern, break the loop
                    break;
                }

                startIndex = text.IndexOf(startPattern);
            }

            return text;
        }
        public async Task SendWhatsAppMessage(SendMessageBody data)
        {
            if (string.IsNullOrEmpty(data.Message)) return;

            try
            {
               
                var payload = new StringContent(
                    JsonSerializer.Serialize(new
                    {
                        messaging_product = "whatsapp",
                        recipient_type = "individual",
                        to = data.To,
                        type = "text",
                        text = new
                        {
                            preview_url = false,
                            body = RemoveSourceReference(data.Message)
                        }
                    }),
                    Encoding.UTF8,
                    "application/json"
                );

                var client = _clientFactory.CreateClient();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _token);

                var response = await client.PostAsync(_url, payload);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("WhatsApp message has been sent.");
                }
                else
                {
                    _logger.LogWarning("WhatsApp message failed to send with status code: " + response.StatusCode);
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError($"Unable to send WhatsApp message. Reason: {ex.Message}");
            }
        }
    }

}
