using Kaizen.Entities;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;

namespace Kaizen.API
{
    public class GetThread
    {
        private readonly ILogger _logger;
        private readonly DataService _aiAssistant;

        public GetThread(ILoggerFactory loggerFactory, DataService aiAssistant)
        {
            _logger = loggerFactory.CreateLogger<GetThread>();
            _aiAssistant = aiAssistant;
        }

        [Function("Thread")]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = "thread/{id}")] HttpRequestData req, string id)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");

            APIGeneralResponse<ThreadRecord> data = new APIGeneralResponse<ThreadRecord>();
            var response = req.CreateResponse(HttpStatusCode.OK);
            try
            {
                data.Data = await _aiAssistant.ThreadRecord(id);
                data.Status = true;
            }
            catch (Exception ex)
            {
                data.Message = ex.Message;
            }

            await response.WriteAsJsonAsync(data);

            return response;
        }
    }
}
