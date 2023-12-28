using System.Net;
using System.Net.Http.Headers;
using System.Net.Http;
using Amazon.Runtime.Internal.Endpoints.StandardLibrary;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using SharpCompress.Common;

namespace Kaizen.API
{
    public class Download
    {
        private readonly ILogger _logger;

        public Download(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<Download>();
        }

        [Function("Download")]
        public HttpResponseData Run([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData req)
        {
            var client = new HttpClient();


            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "EAAMUZADUs6M4BO10RURA9urY500LrBCTR8HVXDdpFwPnZBt2nQMDwvkQ7nS81bzuj4ANASAtLY5bpdmcpLhyHu7UdkuGvB7BiusRY1YiZAYwRQZBIQU6iuClpDXVePhUASWZAysZB4MJ3BfulYBnvEAnTZBvttDVvMnDHnUKEQH1ZAwW68IY8XP9MyTTucpS73tC");
            client.DefaultRequestHeaders.CacheControl = new CacheControlHeaderValue
            {
                NoCache = true,
            };
            client.DefaultRequestHeaders.Add("User-Agent", "PostmanRuntime/7.36.0");
            HttpResponseMessage response =  client.GetAsync("https://lookaside.fbsbx.com/whatsapp_business/attachments/?mid=1583555272051410&ext=1703765402&hash=ATtkDWGnq7JLb3mI6_zzQ64HyX8Ui5srzCHebwjkHTmswg").Result;
            response.EnsureSuccessStatusCode();

            var data =  response.Content.ReadAsStream();
            var responsex = req.CreateResponse(HttpStatusCode.OK);

           
           //  File.wri("downloaded_media.jpg", data);
            using (var fs = new FileStream("file.jpg", FileMode.Create))
            {
                data.CopyTo(fs);
            }
            responsex.WriteString("Done");

            return responsex;
        }
    }
}
