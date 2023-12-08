using System.Net;
using System.Text.Json;
using Azure.Messaging.ServiceBus;
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

        public OnWhatsAppMessageRecieved(
            ILoggerFactory loggerFactory,
            AIAssistant aIAssistant,
            DataService dataService,
            WhatsAppService whatsAppService) // Inject WhatsAppService
        {
            _logger = loggerFactory.CreateLogger<OnWhatsAppMessageRecieved>();
            _aIAssistant = aIAssistant;
            _dataService = dataService;
            _whatsAppService = whatsAppService; // Assign to field
        }

        [Function("OnWhatsAppMessageReceived")]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData req)
        {
            // Create a response with default status of OK
            var response = req.CreateResponse(HttpStatusCode.OK);
            APIGeneralResponse<bool> data = new APIGeneralResponse<bool>();

            try
            {
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


                // Initialize necessary variables
                string aiMessage = string.Empty;
                string message = string.Empty;
                string from = string.Empty;
                string number = string.Empty;

                // Deserialize the request payload
                var root = await req.ReadFromJsonAsync<MetaWebhookModel>();

                // Log the incoming payload for debugging purposes
                _logger.LogInformation("Received payload: {Payload}", JsonSerializer.Serialize(root));

                try
                {
                    // Extract message details from the payload
                    message = root.entry[0].changes[0].value.messages[0].text.body;
                    from = root.entry[0].changes[0].value.messages[0].from;
                    number = root.entry[0].changes[0].value.metadata.display_phone_number;
                }
                catch
                {
                    // Log and return if message extraction fails
                    _logger.LogWarning("Failed to extract message details from payload.");
                    return response;
                }

                // Proceed only if a message is present
                if (!string.IsNullOrEmpty(message))
                {
                    // Retrieve Assistant ID linked with the platform
                    var assistantId = await _dataService.GetAssistantLinkedWithPlatform(number, ConversationPlatform.WhatsApp);

                    // Check if a thread exists against the ID and platform
                    var thread = await _dataService.ThreadExistsInDatabaseAgainstIdAndPlatform(assistantId, from);
                    if (thread == null)
                    {
                        ServiceBusClient client = new ServiceBusClient(Environment.GetEnvironmentVariable("BAServiceBus"));
                        // Handle the case where the thread does not exist
                        // Create thread metadata and a new thread record
                        var metadata = new Dictionary<string, string>
                        {
                            ["AssistantId"] = assistantId,
                            ["Platform"] = Enum.GetName(typeof(ConversationPlatform), ConversationPlatform.WhatsApp),
                            ["PlatformUserId"] = from
                        };

                        var openAiThread = await _aIAssistant.CreateThread(assistantId, metadata);

                        // Send the thread record to the service bus for processing
                        var createThreadRequest = client.CreateSender("create-thread-record");
                        await createThreadRequest.SendMessageAsync(new ServiceBusMessage(JsonSerializer.Serialize(new ThreadRecord
                        {
                            AiMode = true,
                            AssistantId = assistantId,
                            Platform = ConversationPlatform.WhatsApp,
                            PlatformUserId = from,
                            ThreadId = openAiThread.id
                        })));

                        // Get AI response for the new thread
                        aiMessage = await _aIAssistant.GetAIResponse(assistantId, openAiThread.id, message);
                    }
                    else
                    {
                        // Handle existing thread
                        if (thread.AiMode)
                        {
                            // Get AI response if in AI mode
                            aiMessage = await _aIAssistant.GetAIResponse(thread.AssistantId, thread.ThreadId, message);
                        }
                        else
                        {
                            // Add message to thread if not in AI mode
                            await _aIAssistant.AddMessageToThread(new MessageRequest { Assistant_Id = thread.AssistantId, Message = message, Thread_Id = thread.ThreadId });
                        }
                    }

                    // Send AI response back to WhatsApp if there is a message to send
                    if (!string.IsNullOrEmpty(aiMessage))
                    {
                        await _whatsAppService.SendWhatsAppMessage(new SendMessageBody { From = number, Message = aiMessage, To = from });
                    }
                }
                else
                {
                    // Log and handle the case where there is no message
                    data.Message = "No content for processing found in the request.";
                    data.Status = false;
                    _logger.LogWarning(data.Message);
                }
            }
            catch (Exception ex)
            {
                // Log any exceptions that occur during the processing
                data.Message = $"An error occurred: {ex.Message}";
                data.Status = false;
                _logger.LogError(ex, "An exception occurred while processing the WhatsApp message.");
            }

            // Write the API general response to the HTTP response
            await response.WriteAsJsonAsync(data);
            return response;
        }







    }
}