using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace Kaizen.API
{
    public class TimeTest
    {
        private readonly ILogger _logger;

        public TimeTest(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<TimeTest>();
        }

        [Function("TimeTest")]
        public HttpResponseData Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "text/plain; charset=utf-8");
            var now = DateTime.UtcNow;
            _logger.LogWarning(now.ToString("yyyy-MM-dd HH:mm:ss"));
           var dxb = ConvertUtcToDubaiTime(now);
            response.WriteString($"System:{now.ToString("yyyy-MM-dd HH:mm:ss")} DXB:{dxb.ToString("yyyy-MM-dd HH:mm:ss")}");

            return response;
        }

        public static DateTime ConvertUtcToDubaiTime(DateTime utcDateTime)
        {
            TimeZoneInfo dubaiTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Arabian Standard Time");
            DateTime dubaiTime = TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, dubaiTimeZone);
            return dubaiTime;
        }
    }
}
