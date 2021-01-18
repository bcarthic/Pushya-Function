using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Net.Http;

namespace Pushya
{
    public static class Pushya
    {
        [FunctionName("Pushya")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req,
            ILogger log,
            ExecutionContext context)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            try
            {
                string json = string.Empty;
                string jsonPath = Path.Combine(context.FunctionAppDirectory, "dates.json");
                using (StreamReader r = new StreamReader(jsonPath))
                {
                    json = r.ReadToEnd();
                }

                return new JsonResult(json);
            }
            catch (Exception)
            {
                return new JsonResult("");
            }
        }

        [FunctionName("Events")]
        public static async Task<IActionResult> RunEvents(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req,
            ILogger log,
            ExecutionContext context)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            try
            {
                var connectionString = Environment.GetEnvironmentVariable("ConnectionString");
                var containerName = Environment.GetEnvironmentVariable("ContainerName");


                var blobService = new BlobStorageService(connectionString, containerName);
                var events = await blobService.GetBlobs();
                return new JsonResult(JsonConvert.SerializeObject(events));
            }
            catch (Exception)
            {
                return new JsonResult("");
            }

        }

        [FunctionName("Tokens")]
        public static async Task<IActionResult> PostToken(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest req,
            ILogger log,
            ExecutionContext context)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            try
            {
                var content = await new StreamReader(req.Body).ReadToEndAsync();
                var token = JsonConvert.DeserializeObject<Token>(content);

                if (token == null || string.IsNullOrEmpty(token.Id) || string.IsNullOrEmpty(token.TokenValue))
                {
                    return new StatusCodeResult(StatusCodes.Status400BadRequest);
                }

                var connectionString = Environment.GetEnvironmentVariable("ConnectionString");
                var containerName = Environment.GetEnvironmentVariable("TokenContainerName");


                var blobService = new BlobStorageService(connectionString, containerName);
                using (var memoryStream = new MemoryStream())
                {
                    await blobService.UploadAsync(memoryStream, token);
                }

                return new OkObjectResult("Token uploaded");
            }
            catch (Exception)
            {
                return new StatusCodeResult(StatusCodes.Status400BadRequest);
            }
        }
    }
}
