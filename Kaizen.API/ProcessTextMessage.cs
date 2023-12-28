using System;
using System.Text;
using System.Text.Json;
using System.Threading;
using Azure.Messaging.ServiceBus;
using Kaizen.Entities;
using Microsoft.Azure.Amqp.Framing;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Kaizen.API
{
    public class ProcessTextMessage
    {
        private readonly ILogger<ProcessTextMessage> _logger;
        private readonly AIAssistant _aIAssistant;
        private readonly DataService _dataService;
        private readonly WhatsAppService _whatsAppService; // New Service
        private readonly IWebPubSubService _webPubSubService;

        public ProcessTextMessage(ILogger<ProcessTextMessage> logger, AIAssistant aIAssistant,
            DataService dataService,
            WhatsAppService whatsAppService,
            IWebPubSubService webPubSubService)
        {
            _logger = logger;
            _aIAssistant = aIAssistant;
            _dataService = dataService;
            _whatsAppService = whatsAppService;
            _webPubSubService = webPubSubService;
        }

        [Function(nameof(ProcessTextMessage))]
        public async Task Run([ServiceBusTrigger("process-message", Connection = "ServiceBus")] ServiceBusReceivedMessage message)
        {
            var data = JsonSerializer.Deserialize<ProcessMessageModel>(message.Body); 
            if (!string.IsNullOrEmpty(data.message))
            {
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
                        LastActivityAt = DateTime.UtcNow, Alias= data.name
                    })));

                    // Get AI response for the new thread
                    await _aIAssistant.AddMessageToThread(new MessageRequest { Assistant_Id = assistantId, Message =data. message, Thread_Id = openAiThread.id });
                    await _webPubSubService.MessageRecieved(openAiThread.id);

                    data.aiMessage = await _aIAssistant.GetAIResponse(assistantId, openAiThread.id);
                    data.aiThread = openAiThread.id;
                }
                else
                {
                    await _dataService.UpdateThreadRecordActivity(thread.ThreadId);
                    // Handle existing thread
                    if (thread.AiMode)
                    {
                        // Get AI response if in AI mode
                        await _aIAssistant.AddMessageToThread(new MessageRequest { Assistant_Id = assistantId, Message = data.message, Thread_Id = thread.ThreadId });
                        await _webPubSubService.MessageRecieved(thread.ThreadId);
                        data.aiMessage = await _aIAssistant.GetAIResponse(thread.AssistantId, thread.ThreadId);
                        data.aiThread = thread.ThreadId;
                    }
                    else
                    {
                        // Add message to thread if not in AI mode
                        await _aIAssistant.AddMessageToThread(new MessageRequest { Assistant_Id = thread.AssistantId, Message = data.message, Thread_Id = thread.ThreadId });
                        await _webPubSubService.MessageRecieved(thread.ThreadId);
                    }
                }

                // Send AI response back to WhatsApp if there is a message to send
                if (!string.IsNullOrEmpty(data.aiMessage))
                {

                    await _whatsAppService.SendWhatsAppMessage(new SendMessageBody { From = data.number, Message = data.aiMessage, To = data.from });
                    await _webPubSubService.MessageRecieved(data.aiThread);
                }
            }
            else
            {
                _logger.LogWarning("No content for processing found in the request.");
            }
        }

    }
}
