using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Sheesh3Bot.Functions
{
    public static class ACKPing
    {
        [FunctionName("ACKPing")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = "v1")] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("Handling ping request.");
            try
            {
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                dynamic data = JsonConvert.DeserializeObject(requestBody);
                int type = data?.type;

                return new OkObjectResult(new { type = 1 });
            }
            catch (Exception)
            {
                return new BadRequestObjectResult(new { error = "Must provide ping type" });
            }
        }
    }
}
