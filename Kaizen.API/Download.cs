using System.Net;
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
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData req)
        {
            var client = new HttpClient();
           client.DefaultRequestHeaders.Add("Authorization", "Bearer EAAMUZADUs6M4BO10RURA9urY500LrBCTR8HVXDdpFwPnZBt2nQMDwvkQ7nS81bzuj4ANASAtLY5bpdmcpLhyHu7UdkuGvB7BiusRY1YiZAYwRQZBIQU6iuClpDXVePhUASWZAysZB4MJ3BfulYBnvEAnTZBvttDVvMnDHnUKEQH1ZAwW68IY8XP9MyTTucpS73tC");

            HttpResponseMessage response = await client.get("https://lookaside.fbsbx.com/whatsapp_business/attachments/?mid=1583555272051410&ext=1703756035&hash=ATv8z746dDoervyLVeM5_C9BzwehP2ouTZhtPih0PBlJSw");
            response.EnsureSuccessStatusCode();

            byte[] data = await response.Content.ReadAsByteArrayAsync();
            var responsex = req.CreateResponse(HttpStatusCode.OK);

           
            await File.WriteAllBytesAsync("downloaded_media.jpg", data);

            responsex.WriteString("Done");

            return responsex;
        }
    }
}
