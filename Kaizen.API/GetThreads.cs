using System.Net;
using Amazon.Auth.AccessControlPolicy.ActionIdentifiers;
using Kaizen.Entities;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace Kaizen.API
{
    public class GetThreads
    {
        private readonly ILogger _logger;
        private readonly DataService _aiAssistant;

        public GetThreads(ILoggerFactory loggerFactory, DataService aiAssistant)
        {
            _logger = loggerFactory.CreateLogger<GetThreads>();
            _aiAssistant = aiAssistant;
        }

        [Function("Threads")]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post",Route = "threads/{assistantId}")] HttpRequestData req, string assistantId)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");

            APIGeneralResponse<List<ThreadRecord>> data = new APIGeneralResponse<List<ThreadRecord>>();
            var response = req.CreateResponse(HttpStatusCode.OK);
            try
            {
                var searchedText = req.Query["search"].ToString();
                data.Data = new List<ThreadRecord>();
                data.Data = await _aiAssistant.AssistantThreads(assistantId,searchedText);
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
