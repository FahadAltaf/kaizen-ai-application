using System.Net;
using Kaizen.Entities;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace Kaizen.API
{
    public class UpdateRecord
    {
        private readonly ILogger _logger;
        private readonly DataService _dataService;
        private readonly IWebPubSubService _webPubSubService;
        public UpdateRecord(ILoggerFactory loggerFactory, DataService dataService, IWebPubSubService webPubSubService)
        {
            _logger = loggerFactory.CreateLogger<UpdateRecord>();
            _dataService = dataService;
            _webPubSubService = webPubSubService;
        }

        [Function("UpdateRecord")]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData req)
        {
            var response = req.CreateResponse(HttpStatusCode.OK);
            APIGeneralResponse<ThreadRecord> data = new APIGeneralResponse<ThreadRecord>();
            try
            {

                var thread = await req.ReadFromJsonAsync<ThreadRecord>();
                if (thread != null)
                {
                    
                    data.Data = await _dataService.UpdateThread(thread);
                    data.Status = true;
                   await _webPubSubService.AgentJoined(thread.ThreadId);
                }

            }
            catch (Exception ex)
            {
                data.Message = ex.Message;
                _logger.LogError("Unable to create thread record. Reason: " + ex.Message);
            }
            await response.WriteAsJsonAsync(data);
            return response;
        }
    }
}
