using System;
using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Kaizen.Entities;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Kaizen.API
{
    public class CreateThreadRecord
    {
        private readonly ILogger<CreateThreadRecord> _logger;
        private readonly DataService _dataService;

        public CreateThreadRecord(ILogger<CreateThreadRecord> logger, DataService dataService)
        {
            _logger = logger;
            _dataService = dataService;
        }

        [Function(nameof(CreateThreadRecord))]
        public async Task Run([ServiceBusTrigger("create-thread-record", Connection = "ServiceBus")] ServiceBusReceivedMessage message)
        {
            try
            {
                _logger.LogInformation("Message Body: {body}", message.Body);
                var thread = JsonSerializer.Deserialize<ThreadRecord>(message.Body);
                if (thread != null)
                {
                    var newThread = await _dataService.CreateThreadRecord(thread);
                    _logger.LogWarning("Thread record has been created. Id: " + newThread.Id);
                }

            }
            catch (Exception ex)
            {
                _logger.LogError("Unable to create thread record. Reason: " + ex.Message);
            }


        }
    }
}
