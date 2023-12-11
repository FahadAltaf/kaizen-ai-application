using System.Net;
using Azure.Messaging.ServiceBus;
using Azure;
using Kaizen.Entities;
using System.Text.Json;
using System.Text;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Threading;

namespace Kaizen.API
{
    public class Chat
    {
        private readonly ILogger _logger;
        private readonly DataService _service;
        private readonly AIAssistant _aiAssistant;

        public Chat(ILoggerFactory loggerFactory, DataService service, AIAssistant aiAssistant)
        {
            _logger = loggerFactory.CreateLogger<Chat>();
            _service = service;
            _aiAssistant = aiAssistant;
        }

        [Function("Chat")]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData req)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");
            ServiceBusClient client = new ServiceBusClient(Environment.GetEnvironmentVariable("ServiceBus"));
            APIGeneralResponse<AIResponse> data = new APIGeneralResponse<AIResponse>();
            var response = req.CreateResponse(HttpStatusCode.OK);
            try
            {
                data.Data = new AIResponse();
                var record = await req.ReadFromJsonAsync<ConversationModel>();
                _logger.LogWarning("Request recieved: " + await req.ReadAsStringAsync());

                if (string.IsNullOrEmpty(record.AssistantId))
                    throw new Exception("Assistant id is not provided");

                if (string.IsNullOrEmpty(record.content))
                    throw new Exception("Message is not provided");

                if (string.IsNullOrEmpty(record.ThreadId))
                {
                    _logger.LogWarning("Create new thread");
                    var metadata = new Dictionary<string, string>();
                    metadata.Add("AssistantId", record.AssistantId);
                    metadata.Add("Platform", Enum.GetName(typeof(ConversationPlatform), ConversationPlatform.Website));
                    metadata.Add("PlatformUserId", "");

                    var openAiThread = await _aiAssistant.CreateThread(record.AssistantId, metadata);
                    record.ThreadId = openAiThread.id;
                    var notificationQueue = client.CreateSender("create-thread-record");
                    await notificationQueue.SendMessageAsync(new ServiceBusMessage(JsonSerializer.Serialize(new ThreadRecord { AiMode = true, AssistantId = record.AssistantId, Platform = ConversationPlatform.Website, PlatformUserId = "", ThreadId = openAiThread.id })));
                    await _aiAssistant.AddMessageToThread(new MessageRequest { Assistant_Id = record.AssistantId, Message = record.content, Thread_Id = openAiThread.id });
                    data.Data.AIMessage = await _aiAssistant.GetAIResponse(record.AssistantId, openAiThread.id);
                }
                else
                {
                    _logger.LogWarning("Checking record exists? with threadid: " + record.ThreadId);
                    var dbThread = await _service.ThreadRecord(record.AssistantId, record.ThreadId);
                    if (dbThread == null)
                    {
                        _logger.LogWarning("Thread not found in our database");
                        var metadata = new Dictionary<string, string>();
                        metadata.Add("AssistantId", record.AssistantId);
                        metadata.Add("Platform", Enum.GetName(typeof(ConversationPlatform), ConversationPlatform.Website));
                        metadata.Add("PlatformUserId", "");

                        var openAiThread = await _aiAssistant.CreateThread(record.AssistantId, metadata);
                        record.ThreadId = openAiThread.id;
                        var notificationQueue = client.CreateSender("create-thread-record");
                        await notificationQueue.SendMessageAsync(new ServiceBusMessage(JsonSerializer.Serialize(new ThreadRecord { AiMode = true, AssistantId = record.AssistantId, Platform = ConversationPlatform.Website, PlatformUserId = "", ThreadId = openAiThread.id })));
                        await _aiAssistant.AddMessageToThread(new MessageRequest { Assistant_Id = record.AssistantId, Message = record.content, Thread_Id = openAiThread.id });
                        data.Data.AIMessage = await _aiAssistant.GetAIResponse(record.AssistantId, openAiThread.id);
                    }
                    else
                    {
                        record.ThreadId = dbThread.ThreadId;
                        record.AssistantId = dbThread.AssistantId;
                        _logger.LogWarning("Thread found in our database");
                        if (record.role == "agent")
                        {
                            await _service.UpdateThreadRecord(record.ThreadId, false);
                            _logger.LogWarning("Agent jump in record status updated");
                            await _aiAssistant.AddMessageToThread(new MessageRequest { Assistant_Id = record.AssistantId, Message = record.content, Thread_Id = record.ThreadId }, record.role);
                            if (dbThread.Platform == ConversationPlatform.WhatsApp)
                            {
                                //var sendMessageRequest = client.CreateSender("send-whatsapp-message");
                                //await sendMessageRequest.SendMessageAsync(new ServiceBusMessage(JsonSerializer.Serialize(new SendMessageBody { From = "", Message = record.content, To = dbThread.PlatformUserId })));
                                await SendWhatsAppMessage(new SendMessageBody { From = "", Message = record.content, To = dbThread.PlatformUserId });
                            }
                        }
                        else
                        {
                            if (dbThread.AiMode)
                            {
                                await _aiAssistant.AddMessageToThread(new MessageRequest { Assistant_Id = record.AssistantId, Message = record.content, Thread_Id = dbThread.ThreadId });
                                data.Data.AIMessage = await _aiAssistant.GetAIResponse(dbThread.AssistantId, dbThread.ThreadId);
                            }
                            else
                            {
                                await _aiAssistant.AddMessageToThread(new MessageRequest { Assistant_Id = record.AssistantId, Message = record.content, Thread_Id = record.ThreadId });
                            }
                        }
                    }
                }

                if (record.role == "agent")
                {
                   await _service.UpdateThreadRecordAssistance(record.ThreadId);
                }
                data.Data.ThreadId = record.ThreadId;
                data.Data.UserMessage = record.content;
                data.Status = true;
            }
            catch (Exception ex)
            {
                data.Message = ex.Message;
            }

            await response.WriteAsJsonAsync(data);

            return response;
        }

        public async Task SendWhatsAppMessage(SendMessageBody data)
        {
            try
            {
                if (!string.IsNullOrEmpty(data.Message))
                {
                    //Change this based on from
                    var url = "https://graph.facebook.com/v18.0/193557653834811/messages";
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
