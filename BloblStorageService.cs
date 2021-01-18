using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Pushya
{
    public class BlobStorageService
    {
        private BlobContainerClient containerClient;

        public BlobStorageService(string connectionString, string containerName)
        {
            containerClient = new BlobContainerClient(connectionString, containerName);
        }

        public async Task<IList<Events>> GetBlobs()
        {
            try
            {
                var sasUri = this.GetServiceSasUriForContainer();

                var events = new List<Events>();
                foreach (BlobItem blob in this.containerClient.GetBlobs())
                {
                    var blobClient = this.containerClient.GetBlobClient(blob.Name);
                    BlobProperties properties = await blobClient.GetPropertiesAsync();
                    var clickUrl = properties.Metadata["clickUri"];
                    events.Add(new Events
                    {
                        Id = blob.Name,
                        ClickUrl = clickUrl,
                        ImageUrl = $"{blobClient.Uri.AbsoluteUri}{sasUri.Query}"
                    });
                }

                return events;
            }
            catch (RequestFailedException e)
            {
                Console.WriteLine($"HTTP error code {e.Status}: {e.ErrorCode}");
                Console.WriteLine(e.Message);
                throw;
            }
        }

        public async Task UploadAsync(Stream stream, Token token)
        {
            try
            {
                BlobClient blob = this.containerClient.GetBlobClient(token.Id);
                stream.Position = 0;
                await blob.UploadAsync(stream);
                await this.AddBlobMetadataAsync(blob, token);
            }
            catch (RequestFailedException e)
            {
                Console.WriteLine($"HTTP error code {e.Status}: {e.ErrorCode}");
                Console.WriteLine(e.Message);
                throw;
            }
        }

        private async Task AddBlobMetadataAsync(BlobClient blob, Token token)
        {
            try
            {
                IDictionary<string, string> metadata =
                   new Dictionary<string, string>();

                metadata.Add("token", token.TokenValue);
                metadata.Add("name", token.Name);

                // Set the blob's metadata.
                await blob.SetMetadataAsync(metadata);
            }
            catch (RequestFailedException e)
            {
                Console.WriteLine($"HTTP error code {e.Status}: {e.ErrorCode}");
                Console.WriteLine(e.Message);
                throw;
            }
        }

        private Uri GetServiceSasUriForContainer()
        {
            if (this.containerClient.CanGenerateSasUri)
            {
                BlobSasBuilder sasBuilder = new BlobSasBuilder()
                {
                    BlobContainerName = containerClient.Name,
                    Resource = "c"
                };

                sasBuilder.ExpiresOn = DateTimeOffset.UtcNow.AddHours(20);
                sasBuilder.SetPermissions(BlobContainerSasPermissions.Read);

                Uri sasUri = this.containerClient.GenerateSasUri(sasBuilder);
                Console.WriteLine("SAS URI for blob container is: {0}", sasUri);
                Console.WriteLine();

                return sasUri;
            }
            else
            {
                Console.WriteLine(@"BlobContainerClient must be authorized with Shared Key 
                          credentials to create a service SAS.");
                return null;
            }
        }

    }
}
