using System.Net;
using Kaizen.Entities;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace Kaizen.API
{
    public class HandoverToHuman
    {
        private readonly ILogger _logger;
        private readonly DataService _dataservice;
        private readonly IWebPubSubService _webPubSubService;

        public HandoverToHuman(ILoggerFactory loggerFactory, DataService dataservice, IWebPubSubService webPubSubService)
        {
            _logger = loggerFactory.CreateLogger<HandoverToHuman>();
            _dataservice = dataservice;
            _webPubSubService = webPubSubService;
        }

        [Function("HandoverToHuman")]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post",Route = "HandoverToHuman/{threadId}")] HttpRequestData req,string threadId)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");
            APIGeneralResponse<bool> data = new APIGeneralResponse<bool>();
            var response = req.CreateResponse(HttpStatusCode.OK);
            try
            {
                await _dataservice.UpdateThreadRecordAssistance(threadId,true);
                await _webPubSubService.NeedAssistance(threadId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
            data.Status = data.Data = true;
            await response.WriteAsJsonAsync(data);
            return response;
        }
    }
}
