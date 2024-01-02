using System.Net;
using Kaizen.Entities;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace Kaizen.API
{
    public class GetProspects
    {
        private readonly ILogger _logger;
        private readonly DataService _aiAssistant;

        public GetProspects(ILoggerFactory loggerFactory, DataService aiAssistant)
        {
            _logger = loggerFactory.CreateLogger<GetProspects>();
            _aiAssistant = aiAssistant;
        }

        [Function("Prospects")]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData req)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");

            APIGeneralResponse<List<ThreadRecord>> data = new APIGeneralResponse<List<ThreadRecord>>();
            var response = req.CreateResponse(HttpStatusCode.OK);
            try
            {
                data.Data = new List<ThreadRecord>();
                data.Data = await _aiAssistant.AllThreads();
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
