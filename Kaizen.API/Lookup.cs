
using Kaizen.Entities;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Kaizen.API
{
    public class Lookup
    {
        private readonly ILogger _logger;
        private readonly DataService _dataService;

        public Lookup(ILoggerFactory loggerFactory, DataService dataService)
        {
            _logger = loggerFactory.CreateLogger<Lookup>();
            _dataService = dataService;
        }

        [Function("Lookup")]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = "Lookup/{reference}")] HttpRequestData req, string reference)
        {
            APIGeneralResponse<AvailabilityModel> response = new APIGeneralResponse<AvailabilityModel>();
            var apiResponse = req.CreateResponse(System.Net.HttpStatusCode.OK);
            try
            {
                response = await _dataService.RetriveSheets(reference);
               

            }
            catch (Exception ex)
            {
                response.Message = "ERROR";
            }

            await apiResponse.WriteAsJsonAsync(response);
            return apiResponse;
        }
    }
}
