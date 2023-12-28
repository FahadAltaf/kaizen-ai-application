using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace Kaizen.Entities
{
    public class BlobStorageService
    {
        private readonly BlobServiceClient _blobServiceClient;
        private readonly HttpClient _httpClient;

        public BlobStorageService(BlobServiceClient blobServiceClient, HttpClient httpClient)
        {
            _blobServiceClient = blobServiceClient;
            _httpClient = httpClient;
        }

        public async Task<string> DownloadMediaAndUploadToBlobAsync(string mediaUrl, string type, string containerName, string blobName)
        {
            // Download media from the URL
            var request = new HttpRequestMessage(HttpMethod.Get, mediaUrl);
            // Add your authorization headers
            request.Headers.Add("Authorization", $"Bearer {Environment.GetEnvironmentVariable("MetaKey")}");
            request.Headers.Add("User-Agent", "PostmanRuntime/7.36.0");
            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            // Get a reference to a container and then a blob
            var container = _blobServiceClient.GetBlobContainerClient(containerName);
            var blob = container.GetBlobClient(blobName);

            await blob.UploadAsync(await response.Content.ReadAsStreamAsync(), new BlobHttpHeaders { ContentType = type });
            // Return the public URL of the blob
            return blob.Uri.ToString();
        }
    }
}
