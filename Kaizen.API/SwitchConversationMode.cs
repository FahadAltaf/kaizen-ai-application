using System.Net;
using Kaizen.Entities;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace Kaizen.API
{
    public class SwitchConversationMode
    {
        private readonly ILogger _logger;
        private readonly DataService _dataService;

        public SwitchConversationMode(ILoggerFactory loggerFactory, DataService dataService)
        {
            _logger = loggerFactory.CreateLogger<SwitchConversationMode>();
            _dataService = dataService;
        }

        [Function("Switch/{threadId}/{mode}")]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData req, string threadId, string mode)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");
            APIGeneralResponse<bool> data = new APIGeneralResponse<bool>();
            var response = req.CreateResponse(HttpStatusCode.OK);
            try
            {
                await _dataService.UpdateThreadRecord(threadId, Convert.ToBoolean(mode));
                data.Status = true;
            }
            catch (Exception ex)
            {
                data.Message = "Unable to change the conversation Mode. Reason: " + ex.Message;
            }

            await response.WriteAsJsonAsync(data);

            return response;
        }
    }
}
