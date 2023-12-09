using System;
using System.Text.Json;
using System.Text;
using Azure.Messaging.ServiceBus;
using Kaizen.Entities;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Kaizen.API
{
    public class SendWhatsAppMessage
    {
        private readonly ILogger<SendWhatsAppMessage> _logger;

        public SendWhatsAppMessage(ILogger<SendWhatsAppMessage> logger)
        {
            _logger = logger;
        }

        [Function(nameof(SendWhatsAppMessage))]
        public async Task Run([ServiceBusTrigger("send-whatsapp-message", Connection = "ServiceBus")] ServiceBusReceivedMessage message)
        {
            try
            {
                _logger.LogInformation("Message Body: {body}", message.Body);
                var data = JsonSerializer.Deserialize<SendMessageBody>(message.Body);

                if (!string.IsNullOrEmpty(data.Message))
                {
                    //Change this based on from
                    var url = "https://graph.facebook.com/v17.0/133908173119659/messages";
                    var token = Environment.GetEnvironmentVariable("MetaKey");
                    var contentType = "application/json";
                    var payload = new StringContent(
                        JsonSerializer.Serialize(new
                        {
                            messaging_product = "whatsapp",
                            recipient_type = "individual",
                            to = data.To, // Replace with the actual phone number
                            type = "text",
                            text = new
                            {
                                preview_url = false,
                                body = data.Message // Replace with the actual message content
                            }
                        }),
                        Encoding.UTF8,
                        "application/json"
                    );

                    using var client = new HttpClient();
                    client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                    await client.PostAsync(url, payload);
                    _logger.LogWarning("WhatsApp message has been sent");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Unable to send WhatsApp message. Reason: " + ex.Message);
            }


        }
    }
}
