using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;

namespace Sheesh3Bot.Functions
{
    public class KeepAwakeFunc
    {
        private static readonly HttpClient _client = new HttpClient();

        [FunctionName("KeepAwakeFunc")]
        public async Task Run([TimerTrigger("0 */20 * * * *")]TimerInfo myTimer, ILogger log, ExecutionContext context)
        {
            string interactionsUrl = Environment.GetEnvironmentVariable("FUNCTIONS_URL");
            interactionsUrl += "/Interactions";

            await _client.GetAsync(interactionsUrl);

            log.LogInformation($"Woke up InteractionsFunc");
        }
    }
}
