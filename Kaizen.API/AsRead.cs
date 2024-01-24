using System.Net;
using Kaizen.Entities;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace Kaizen.API
{
    public class AsRead
    {
        private readonly ILogger _logger;
        private readonly DataService _dataService;

        public AsRead(ILoggerFactory loggerFactory, DataService dataService)
        {
            _logger = loggerFactory.CreateLogger<AsRead>();
            _dataService = dataService;
        }

        [Function("AsRead")]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = "AsRead/{threadId}/{mode}")] HttpRequestData req, string threadId, string mode)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");
            APIGeneralResponse<ThreadRecord> data = new APIGeneralResponse<ThreadRecord>();
            var response = req.CreateResponse(HttpStatusCode.OK);
            try
            {
                await _dataService.UpdateThreadHasMessages(threadId, Convert.ToBoolean(mode));
                data.Data = await _dataService.ThreadRecord(threadId);
                data.Status = true;
            }
            catch (Exception ex)
            {
                data.Message = "Unable to mark the message as read. Reason: " + ex.Message;
            }

            await response.WriteAsJsonAsync(data);

            return response;
        }
    }
}
