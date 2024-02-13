using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using Azure.Core;
using Azure.Messaging.ServiceBus;
using Azure.Storage.Blobs;
using Kaizen.Entities;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Kaizen.API
{
    public class ProcessMediaMessage
    {
        private readonly ILogger<ProcessMediaMessage> _logger;
        private readonly AIAssistant _aIAssistant;
        private readonly DataService _dataService;
        private readonly WhatsAppService _whatsAppService; // New Service
        private readonly IWebPubSubService _webPubSubService;
        private readonly HttpClient _httpClient;

        public ProcessMediaMessage(ILogger<ProcessMediaMessage> logger, AIAssistant aIAssistant,
            DataService dataService,
            WhatsAppService whatsAppService,
            IWebPubSubService webPubSubService)
        {
            _logger = logger;
            _aIAssistant = aIAssistant;
            _dataService = dataService;
            _whatsAppService = whatsAppService;
            _webPubSubService = webPubSubService;
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {Environment.GetEnvironmentVariable("MetaKey")}");
        }

        [Function(nameof(ProcessMediaMessage))]
        public async Task Run([ServiceBusTrigger("process-media", Connection = "ServiceBus")] ServiceBusReceivedMessage message)
        {
            try
            {
                var data = JsonSerializer.Deserialize<ProcessMessageModel>(message.Body);
                _logger.LogWarning(JsonSerializer.Serialize(data));
                if (!string.IsNullOrEmpty(data.docId))
                {
                    var media = await _httpClient.GetFromJsonAsync<FbMediaModel>($"https://graph.facebook.com/v18.0/{data.docId}");
                    if (media != null && !string.IsNullOrEmpty(media.url))
                    {
                        //Download the media
                        BlobStorageService service = new BlobStorageService(new Azure.Storage.Blobs.BlobServiceClient(Environment.GetEnvironmentVariable("StorageKey")), _httpClient);
                        var fileName = $"{media.id}.{GetFileExtensionFromMimeType(media.mime_type)}";
                        _logger.LogInformation(fileName);
                        data.docId = await service.DownloadMediaAndUploadToBlobAsync(media.url, media.mime_type, "kaizen-media", fileName);
                        _logger.LogWarning(data.docId);
                        // Retrieve Assistant ID linked with the platform
                        var assistantId = await _dataService.GetAssistantLinkedWithPlatform(data.number, ConversationPlatform.WhatsApp);

                        // Check if a thread exists against the ID and platform
                        var thread = await _dataService.ThreadExistsInDatabaseAgainstIdAndPlatform(assistantId, data.from);
                        if (thread == null)
                        {
                            ServiceBusClient client = new ServiceBusClient(Environment.GetEnvironmentVariable("ServiceBus"));
                            // Handle the case where the thread does not exist
                            // Create thread metadata and a new thread record
                            var metadata = new Dictionary<string, string>
                            {
                                ["AssistantId"] = assistantId,
                                ["Platform"] = Enum.GetName(typeof(ConversationPlatform), ConversationPlatform.WhatsApp),
                                ["PlatformUserId"] = data.from
                            };

                            var openAiThread = await _aIAssistant.CreateThread(assistantId, metadata);

                            // Send the thread record to the service bus for processing
                            var createThreadRequest = client.CreateSender("create-thread-record");
                            await createThreadRequest.SendMessageAsync(new ServiceBusMessage(JsonSerializer.Serialize(new ThreadRecord
                            {
                                AiMode = true,
                                AssistantId = assistantId,
                                Platform = ConversationPlatform.WhatsApp,
                                PlatformUserId = data.from,
                                ThreadId = openAiThread.id,
                                LastActivityAt = DateTime.UtcNow,
                                Alias = data.name
                            })));

                            // Get AI response for the new thread
                            await _aIAssistant.AddMessageToThread(new MessageRequest { Assistant_Id = assistantId, Message = data.message, Thread_Id = openAiThread.id, docId = data.docId }, new Dictionary<string, string> { });
                            await _webPubSubService.MessageRecieved(openAiThread.id);

                            data.aiMessage = "";//await _aIAssistant.GetAIResponse(assistantId, openAiThread.id);
                            data.aiThread = openAiThread.id;
                        }
                        else
                        {
                            LastMesageBy by = LastMesageBy.AI;
                            // Handle existing thread
                            if (thread.AiMode)
                            {
                                // Get AI response if in AI mode
                                await _aIAssistant.AddMessageToThread(new MessageRequest { Assistant_Id = assistantId, Message = data.message, Thread_Id = thread.ThreadId, docId = data.docId }, new Dictionary<string, string> { });
                                await _webPubSubService.MessageRecieved(thread.ThreadId);
                                //data.aiMessage = await _aIAssistant.GetAIResponse(thread.AssistantId, thread.ThreadId);
                                data.aiThread = thread.ThreadId;
                            }
                            else
                            {
                                by = LastMesageBy.User;
                                // Add message to thread if not in AI mode
                                await _aIAssistant.AddMessageToThread(new MessageRequest { Assistant_Id = thread.AssistantId, Message = data.message, Thread_Id = thread.ThreadId, docId = data.docId }, new Dictionary<string, string> { });
                                await _webPubSubService.MessageRecieved(thread.ThreadId);
                            }

                            await _dataService.UpdateThreadRecordActivity(thread.ThreadId,by);
                        }
                    }
                    else
                    {
                        _logger.LogWarning("No media id.");
                    }
                }
                else
                {
                    _logger.LogWarning("No content for processing found in the request.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }

        }

        string GetFileExtensionFromMimeType(string mimeType)
        {
            switch (mimeType)
            {
                case "image/jpeg":
                    return ".jpg";
                // Add other MIME types and corresponding extensions
                case "image/png":
                    return ".png";
                case "application/pdf":
                    return ".pdf";
                case "audio/mp4":
                    return ".mp4";
                case "audio/mpeg":
                    return ".mpeg";
                case "audio/ogg":
                    return ".ogg";
                case "application/msword":
                    return ".docx";
                case "application/vnd.ms-excel":
                    return ".xlsx";
                default:
                    return "";
            }
        }
    }
}
