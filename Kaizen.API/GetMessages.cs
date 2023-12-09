using System.Net;
using Kaizen.Entities;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace Kaizen.API
{
    public class GetConversationMessages
    {
        private readonly ILogger _logger;
        private readonly AIAssistant _aiAssistant;

        public GetConversationMessages(ILoggerFactory loggerFactory, AIAssistant aiAssistant)
        {
            _logger = loggerFactory.CreateLogger<GetConversationMessages>();
            _aiAssistant = aiAssistant;
        }

        [Function("conversation")]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post",Route = "conversation/{threadId}")] HttpRequestData req, string threadId)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");

            APIGeneralResponse<List<Conversations>> data = new APIGeneralResponse<List<Conversations>>();
            var response = req.CreateResponse(HttpStatusCode.OK);
            try
            {
                data.Data = new List<Conversations>();
                foreach (var msg in await _aiAssistant.GetMessages(threadId))
                {
                    if (msg.role == "assistant")
                    {
                        msg.metadata.Add("role", "assistant");
                    }
                    data.Data.Add(new Conversations { content = msg.content[0].text.value, role = msg.role, metadata = msg.metadata });
                }
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

