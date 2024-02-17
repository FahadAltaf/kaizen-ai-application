using System;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
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
            try
            {
                string prompt = "You are Sandy, a support agent for Kaizen Unit Management Service. Always start conversation by saying \"Welcome to Kaizen unit services. How may I help you\" and dont provide any details in first message. Maintain a professional and supportive tone. You have access to JSON data with property details and FAQs relevant to user inquiries. Use this information to provide accurate, concise answers. If information is not available, inform the user that their question has been forwarded for further assistance. Avoid assumptions; prioritize accuracy and simplicity in your responses. Again keep your answers short as much as possible.";
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
                        bool isLeasing = (data.message.Contains("https://www.propertyfinder.ae/to") || data.message.Contains("https://dubizzle.com/s") || data.message.Contains("https://www.bayut.com/pm") || data.message.Contains("https://www.bayut.com/property"));
                        AvailabilityModel availability = new AvailabilityModel();
                        string refNumber = "";
                        if (isLeasing)
                        {
                            refNumber = ExtractReferenceNumber(data.message);
                            var service = await _dataService.RetriveSheets(refNumber);
                            if (service.Status)
                            {
                                availability = service.Data;
                            }
                            else
                            {
                                availability = null;
                            }
                        }

                        // Send the thread record to the service bus for processing
                        var createThreadRequest = client.CreateSender("create-thread-record");
                        await createThreadRequest.SendMessageAsync(new ServiceBusMessage(JsonSerializer.Serialize(new ThreadRecord
                        {
                            AiMode = (availability == null) ? false : true,
                            AssistantId = assistantId,
                            Platform = ConversationPlatform.WhatsApp,
                            PlatformUserId = data.from,
                            ThreadId = openAiThread.id,
                            LastActivityAt = DateTime.UtcNow,
                            Alias = data.name,
                            IsLeasing = isLeasing,
                            LastMesageBy = LastMesageBy.AI,
                            PropRef = (availability == null) ? "" : refNumber
                        })));

                        // Get AI response for the new thread
                        await _aIAssistant.AddMessageToThread(new MessageRequest { Assistant_Id = assistantId, Message = data.message, Thread_Id = openAiThread.id }, new Dictionary<string, string> { });
                        await _webPubSubService.MessageRecieved(openAiThread.id);
                        if (isLeasing)
                        {
                            if (availability != null)
                            {
                                string inst = $"{prompt}\n\n{JsonSerializer.Serialize(availability)}";
                                data.aiMessage = await _aIAssistant.GetAIResponse("asst_9WElSN4fGJbXBwFESoKqPlP8", openAiThread.id, inst);
                            }
                            else
                            {
                                await Task.Delay(1000);
                                data.aiMessage = IsOfficeHours(_logger) ? "Hello! Welcome to Kaizen. Thank you for your interest in our property. I have forwarded your question to one of our agents. Someone will be soon in contact with you."
                                    : "Hello,\r\nThank you for reaching out to us! Our office hours are from 9 am to 6 pm, Monday to Saturday. Please note that our team is currently away. Rest assured, your message is important to us. The concerned person will reach out to you on the next working day.\r\nThank you for your understanding.";
                                await _aIAssistant.AddMessageToThread(new MessageRequest { Assistant_Id = assistantId, Message = data.aiMessage, Thread_Id = openAiThread.id }, new Dictionary<string, string> { }, "assistant");
                            }
                        }
                        else
                        {
                            data.aiMessage = await _aIAssistant.GetAIResponse(assistantId, openAiThread.id);
                        }

                        data.aiThread = openAiThread.id;
                    }
                    else
                    {
                        bool isLeasing = thread.IsLeasing; //(data.message.Contains("https://www.propertyfinder.ae/to") || data.message.Contains("https://dubizzle.com/s") || data.message.Contains("https://www.bayut.com/pm") || data.message.Contains("https://www.bayut.com/property"));

                        AvailabilityModel availability = new AvailabilityModel();
                        if (isLeasing)
                        {
                            var service = await _dataService.RetriveSheets(thread.PropRef);
                            if (service.Status)
                            {
                                availability = service.Data;
                            }
                            else
                            {
                                availability = null;
                            }
                        }
                        LastMesageBy by = LastMesageBy.AI;
                        // Handle existing thread
                        if (thread.AiMode)
                        {
                            // Get AI response if in AI mode
                            await _aIAssistant.AddMessageToThread(new MessageRequest { Assistant_Id = assistantId, Message = data.message, Thread_Id = thread.ThreadId }, new Dictionary<string, string> { });
                            await _webPubSubService.MessageRecieved(thread.ThreadId);
                            if (isLeasing)
                            {
                                if (availability != null)
                                {
                                    string inst = $"{prompt}\n\n{JsonSerializer.Serialize(availability)}";
                                    data.aiMessage = await _aIAssistant.GetAIResponse("asst_9WElSN4fGJbXBwFESoKqPlP8", thread.ThreadId, inst);
                                }
                                else
                                {
                                    data.aiMessage = IsOfficeHours(_logger) ? "Hello! Welcome to Kaizen. Thank you for your interest in our property. I have forwarded your question to one of our agents. Someone will be soon in contact with you."
                                : "Hello,\r\nThank you for reaching out to us! Our office hours are from 9 am to 6 pm, Monday to Saturday. Please note that our team is currently away. Rest assured, your message is important to us. The concerned person will reach out to you on the next working day.\r\nThank you for your understanding.";
                                    await _aIAssistant.AddMessageToThread(new MessageRequest { Assistant_Id = thread.AssistantId, Message = data.aiMessage, Thread_Id = thread.ThreadId }, new Dictionary<string, string> { }, "assistant");
                                }
                            }
                            else
                            {
                                if (!thread.IsLeasing)
                                {
                                    data.aiMessage = await _aIAssistant.GetAIResponse(thread.AssistantId, thread.ThreadId);
                                }

                            }

                            data.aiThread = thread.ThreadId;
                        }
                        else
                        {
                            by = LastMesageBy.User;
                            // Add message to thread if not in AI mode
                            await _aIAssistant.AddMessageToThread(new MessageRequest { Assistant_Id = thread.AssistantId, Message = data.message, Thread_Id = thread.ThreadId }, new Dictionary<string, string> { });
                            await _dataService.UpdateThreadHasMessages(thread.ThreadId, true);
                            await _webPubSubService.NewMessage(thread.ThreadId);
                            await _webPubSubService.MessageRecieved(thread.ThreadId);
                        }

                        await _dataService.UpdateThreadRecordActivity(thread.ThreadId, by);
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
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
            
        }
        public static bool IsOfficeHours(ILogger logger)
        {
            DateTime utcDateTime = DateTime.UtcNow;
            TimeZoneInfo dubaiTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Arabian Standard Time");
            DateTime dubaiTime = TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, dubaiTimeZone);
            logger.LogWarning("Dubai time: " + dubaiTime.ToString("yyyy-MM-dd HH:mm:ss"));
            if (dubaiTime.Hour >= 9 && dubaiTime.Hour <= 18)
            {
                return (dubaiTime.DayOfWeek == DayOfWeek.Sunday) ? false : true;
            }
            else
                return false;
        }

        static string ExtractReferenceNumber(string text)
        {
            // The regex pattern for matching the reference number format
            string pattern = @"\b[A-Z]+-[A-Z]+-[A-Z0-9]+\b";

            // Create a Regex object
            Regex regex = new Regex(pattern);

            // Search the input text for the first occurrence that matches the regex pattern
            Match match = regex.Match(text);

            // If a match is found, return the matched value, otherwise return null
            return match.Success ? match.Value : null;
        }
    }
}
