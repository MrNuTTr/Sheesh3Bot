using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Net;
using System.Net.Http;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http.Extensions;
using System.Text;

namespace Sheesh3Bot.Functions
{
    public static class Authenticate
    {
        static readonly HttpClient client = new HttpClient();

        static readonly string discord_id = Environment.GetEnvironmentVariable("DISCORD_API_ID");
        static readonly string discord_key = Environment.GetEnvironmentVariable("DISCORD_API_KEY");

        [FunctionName("Authenticate")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = "redirect")] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("Received redirect from Discord.");

            string code = req.Query["code"];
            string guild = req.Query["guild_id"]; // might need this later
            
            var values = new Dictionary<string, string>
            {
                {"client_id", discord_id},
                {"client_secret", discord_key},
                {"response_type", "code"},
                {"code", code},
                {"redirect_uri", req.GetDisplayUrl()}
            };

            var data = new FormUrlEncodedContent(values);

            string url = "https://discordapp.com/api/oauth2/token";

            //var response = await client.PostAsync(url, data);

            return new OkObjectResult(values);
        }
    }
}
