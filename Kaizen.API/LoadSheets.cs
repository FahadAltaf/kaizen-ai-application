using Kaizen.Entities;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;

namespace Kaizen.API
{
    public class LoadSheets
    {
        private readonly ILogger _logger;
        private readonly DataService _dataService;

        public LoadSheets(ILoggerFactory loggerFactory, DataService dataService)
        {
            _logger = loggerFactory.CreateLogger<GetThreads>();
            _dataService= dataService;
        }

        [Function("LoadSheets")]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData req)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");

            APIGeneralResponse<List<GoogleSpreadSheet>> data = new APIGeneralResponse<List<GoogleSpreadSheet>>();
            var response = req.CreateResponse(HttpStatusCode.OK);
            try
            {
                data.Data = await _dataService.GetSheets();
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
