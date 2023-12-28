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
            _httpClient.DefaultRequestHeaders.CacheControl = new CacheControlHeaderValue
            {
                NoCache = true,
                NoStore = true,
                MustRevalidate = true
            };
            // Download media from the URL
            var request = new HttpRequestMessage(HttpMethod.Get, mediaUrl);
            // Add your authorization headers
            request.Headers.Add("Authorization", $"Bearer {Environment.GetEnvironmentVariable("MetaKey")}");
           
            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            // Get a reference to a container and then a blob
            var container = _blobServiceClient.GetBlobContainerClient(containerName);
            var blob = container.GetBlobClient(blobName);
            using (var contentStream = await response.Content.ReadAsStreamAsync())
            {
                // Save the stream to a local file
            
                    await blob.UploadAsync(contentStream, new BlobHttpHeaders { ContentType = type });
                
            }
            

            // Upload the content to the blob
         

            // Optionally set the blob to be publicly accessible
            //await container.SetAccessPolicyAsync(PublicAccessType.Blob);

            // Return the public URL of the blob
            return blob.Uri.ToString();
        }
    }
}
