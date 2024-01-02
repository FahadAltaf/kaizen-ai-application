using System.Net;
using Kaizen.Entities;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Amqp.Framing;

namespace Kaizen.API
{
    public class CreateRecord
    {
        private readonly ILogger _logger;
        private readonly DataService _dataService;
        private readonly AIAssistant _assistant;
        public CreateRecord(ILoggerFactory loggerFactory, DataService dataService, AIAssistant assistant)
        {
            _logger = loggerFactory.CreateLogger<CreateRecord>();
            _dataService = dataService;
            _assistant = assistant;
        }

        [Function("CreateRecord")]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData req)
        {
            var response = req.CreateResponse(HttpStatusCode.OK);
            APIGeneralResponse<ThreadRecord> data = new APIGeneralResponse<ThreadRecord>();
            try
            {

                var thread = await req.ReadFromJsonAsync<ThreadRecord>();
                if (thread != null)
                {
                    thread.AiMode = true;
                    thread.AssistantId = "asst_CkXRdKynHfveoZ22Pg4gfQuw";
                    var dbEntry =await _dataService.ThreadExistsInDatabaseAgainstIdAndPlatform(thread.AssistantId, thread.PlatformUserId);
                    if(dbEntry != null)
                    {
                        data.Message = "Prospect with this number already exists";
                        data.Status = false;
                    }
                    else
                    {
                        var metadata = new Dictionary<string, string>
                        {
                            ["AssistantId"] = thread.AssistantId,
                            ["Platform"] = thread.Platform.ToString(),
                            ["PlatformUserId"] = thread.PlatformUserId
                        };
                        var aiThread = await _assistant.CreateThread(thread.AssistantId, metadata);
                        thread.ThreadId = aiThread.id;
                        data.Data = await _dataService.CreateThreadRecord(thread);
                        data.Status = true;
                    }
                    
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
