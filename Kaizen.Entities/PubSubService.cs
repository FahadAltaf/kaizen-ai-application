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

        public async Task NeedAssistance(string threadId)
        {
            await _client.SendToAllAsync($"needAssistance-{threadId}", Azure.Core.ContentType.TextPlain);
        }

        // Implement the methods
    }

}
