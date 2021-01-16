using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Pushya
{
    public static class Pushya
    {
        [FunctionName("Pushya")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log,
            ExecutionContext context)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string json = string.Empty;
            string jsonPath = Path.Combine(context.FunctionAppDirectory, "dates.json");
            using (StreamReader r = new StreamReader(jsonPath))
            {
                json = r.ReadToEnd();
            }

            return new JsonResult(json);
        }
    }
}
