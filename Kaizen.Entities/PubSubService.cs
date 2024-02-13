using Azure.Messaging.WebPubSub;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kaizen.Entities
{
    public interface IWebPubSubService
    {
        Task MessageRecieved(string threadId);
        Task NeedAssistance(string threadId);
        Task NewMessage(string threadId);
        Task InquiryRecieved(string threadId);
        Task AgentJoined(string threadId);
    }

    public class WebPubSubService : IWebPubSubService
    {
        private readonly WebPubSubServiceClient _client;

        public WebPubSubService(string connectionString, string hubName)
        {
            _client = new WebPubSubServiceClient(connectionString, hubName);
        }

        public async Task MessageRecieved(string threadId)
        {
          await  _client.SendToAllAsync($"messageRecieved-{threadId}", Azure.Core.ContentType.TextPlain);
        }
        public async Task AgentJoined(string threadId)
        {
            await _client.SendToAllAsync($"agentJoined-{threadId}", Azure.Core.ContentType.TextPlain);
        }
        public async Task InquiryRecieved(string threadId)
        {
            await _client.SendToAllAsync($"inquiryRecieved-{threadId}", Azure.Core.ContentType.TextPlain);
        }

        public async Task NeedAssistance(string threadId)
        {
            await _client.SendToAllAsync($"needAssistance-{threadId}", Azure.Core.ContentType.TextPlain);
        }

        public async Task NewMessage(string threadId)
        {
            await _client.SendToAllAsync($"newMessage-{threadId}", Azure.Core.ContentType.TextPlain);
        }

        // Implement the methods
    }

}
