using System.Net;
using System.Text.Json;
using System.Threading;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.WebPubSub;
using Kaizen.Entities;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace Kaizen.API
{
    public class OnWhatsAppMessageRecieved
    {
        private readonly ILogger _logger;
        private readonly AIAssistant _aIAssistant;
        private readonly DataService _dataService;
        private readonly WhatsAppService _whatsAppService; // New Service
        private readonly IWebPubSubService _webPubSubService;
        public OnWhatsAppMessageRecieved(
            ILoggerFactory loggerFactory,
            AIAssistant aIAssistant,
            DataService dataService,
            WhatsAppService whatsAppService,
            IWebPubSubService webPubSubService)
        {
            _logger = loggerFactory.CreateLogger<OnWhatsAppMessageRecieved>();
            _aIAssistant = aIAssistant;
            _dataService = dataService;
            _whatsAppService = whatsAppService;
            _webPubSubService = webPubSubService;
        }

        [Function("OnWhatsAppMessageReceived")]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData req)
        {
            var response = req.CreateResponse(HttpStatusCode.OK);
            APIGeneralResponse<bool> data = new APIGeneralResponse<bool>();

            try
            {
                #region Will only use for whatsapp webhook verification
                var query = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
                var mode = query["hub.mode"];
                var token = query["hub.verify_token"];
                var challenge = query["hub.challenge"];

                if (!string.IsNullOrEmpty(mode))
                {
                    // Your verify token should be a constant string
                    var VERIFY_TOKEN = "Azure";

                    // Checks if a token and mode is in the query string of the request
                    if (mode == "subscribe" && token == VERIFY_TOKEN)
                    {
                        // Responds with the challenge token from the request
                        _logger.LogInformation("WEBHOOK_VERIFIED");
                        response.WriteString(challenge);
                        return response;
                    }
                    else
                    {
                        // Responds with '403 Forbidden' if verify tokens do not match
                        _logger.LogError("Failed to verify webhook, the verify tokens do not match.");
                        response.StatusCode = HttpStatusCode.Forbidden;
                        return response;
                    }
                }
                #endregion

                // Initialize necessary variables
                string aiMessage = string.Empty;
                string aiThread = "";
                string message = string.Empty;
                string from = string.Empty;
                string number = string.Empty;

                var root = await req.ReadFromJsonAsync<MetaWebhookModel>();
                _logger.LogWarning("Received payload: {Payload}", JsonSerializer.Serialize(root));


                // Extract message details from the payload
                switch (root.entry[0].changes[0].value.messages[0].type)
                {
                    case "text":
                        message = root.entry[0].changes[0].value.messages[0].text.body;
                        from = root.entry[0].changes[0].value.messages[0].from;
                        number = root.entry[0].changes[0].value.metadata.display_phone_number;

                        if (!string.IsNullOrEmpty(message))
                        {
                            ServiceBusClient client = new ServiceBusClient(Environment.GetEnvironmentVariable("ServiceBus"));
                            var createThreadRequest = client.CreateSender("process-message");
                            await createThreadRequest.SendMessageAsync(new ServiceBusMessage(JsonSerializer.Serialize(new ProcessMessageModel {
                              aiMessage=aiMessage, aiThread=aiThread, message=message, from = from, number=number
                            })));
                        }
                        
                        break;
                    default:
                        break;
                }
                data.Status = true; data.Data = true;

            }
            catch (Exception ex)
            {
                // Log any exceptions that occur during the processing
                data.Message = $"An error occurred: {ex.Message}";
                data.Status = false;
                _logger.LogError(ex, "An exception occurred while processing the WhatsApp message.");
            }
            finally
            {

            }

            // Write the API general response to the HTTP response
            await response.WriteAsJsonAsync(data);
            return response;
        }
    }
}