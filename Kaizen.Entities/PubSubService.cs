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
        Task MessageRecieved();
    }

    public class WebPubSubService : IWebPubSubService
    {
        private readonly WebPubSubServiceClient _client;

        public WebPubSubService(string connectionString, string hubName)
        {
            _client = new WebPubSubServiceClient(connectionString, hubName);
        }

        public async Task MessageRecieved()
        {
          await  _client.SendToAllAsync("messageRecieved", Azure.Core.ContentType.TextPlain);
        }

        // Implement the methods
    }

}
