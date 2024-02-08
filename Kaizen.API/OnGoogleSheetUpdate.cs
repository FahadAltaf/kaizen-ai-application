using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text;
using Kaizen.Entities;
using Microsoft.Azure.Functions.Worker.Http;
using System.Net.Http.Json;

namespace Kaizen.API
{
    public class OnGoogleSheetUpdate
    {
        private readonly ILogger _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly AIAssistant _assistant;
        private readonly DataService _dataService;
        string assistantId = "asst_9WElSN4fGJbXBwFESoKqPlP8";
        public OnGoogleSheetUpdate(ILoggerFactory loggerFactory, IHttpClientFactory httpClientFactory, AIAssistant assistant, DataService dataService)
        {
            _logger = loggerFactory.CreateLogger<OnGoogleSheetUpdate>();
            _httpClientFactory = httpClientFactory;
            _assistant = assistant;
            _dataService = dataService;
        }

        [Function("OnGoogleSheetUpdate")]
        public async Task<HttpResponseData> Run(
      [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req, FunctionContext executionContext)
        {
            
            APIGeneralResponse<bool> apiResponse = new APIGeneralResponse<bool>();
            var successResponse = req.CreateResponse(System.Net.HttpStatusCode.OK);
            try
            {
                // Read the request body
                var requestBody = await req.ReadAsStringAsync();
_logger.LogWarning(requestBody);
                var jsonDocument = JsonDocument.Parse(requestBody);

                // Extract the 'data' property which is a string representation of an array
                var spreadsheetId = jsonDocument.RootElement.GetProperty("spreadsheetId").GetString();
                var sheetId = jsonDocument.RootElement.GetProperty("sheetId").GetInt64();
                var sheetName = jsonDocument.RootElement.GetProperty("sheetName").GetString();
                var dataString = jsonDocument.RootElement.GetProperty("data").GetString();
                if (sheetId == 247377476)
                {
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };
                    var dataArray = JsonSerializer.Deserialize<JsonElement[][]>(dataString, options);

                    var csvContent = new StringBuilder();
                    foreach (var row in dataArray)
                    {
                        var line = string.Join(";", row.Select(element =>
                        {
                            // Check the type of JsonElement and convert it to a string accordingly
                            return element.ValueKind switch
                            {
                                JsonValueKind.String => element.GetString().Replace(";", ","),
                                JsonValueKind.Number => element.GetRawText(),
                                JsonValueKind.True => "true",
                                JsonValueKind.False => "false",
                                _ => element.GetRawText()
                            };
                        }));
                        csvContent.AppendLine(line);
                    }

                    var sheet = await _dataService.GetSheetRecord(spreadsheetId, sheetId);
                    if (sheet == null)
                    {
                        await _dataService.CreateSheetRecord(new GoogleSpreadSheet { SpreadName=sheetName, SheetData=csvContent.ToString(), SheetId=sheetId, SpreadSheetId= spreadsheetId, UpdatedAt=DateTime.UtcNow  });
                    }
                    else
                    {
                        sheet.SpreadName = sheetName;
                        sheet.SheetData = csvContent.ToString();
                        sheet.UpdatedAt=DateTime.UtcNow;
                        await _dataService.UpdateSheet(sheet);
                    }
                }
                else
                {
                    _logger.LogWarning("Sheet has been ignored");
                }
               
                apiResponse.Status = apiResponse.Data = true;
            }
            catch (Exception ex)
            {
                apiResponse.Message = "Error Reason:" + ex.Message;
                _logger.LogError(ex.Message);
            }

            await successResponse.WriteAsJsonAsync(apiResponse);
            return successResponse;
        }
    }
}
