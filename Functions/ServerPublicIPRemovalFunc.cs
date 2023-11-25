using System;
using System.Threading.Tasks;
using Azure.Data.Tables;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Sheesh3Bot.Services;
using Sheesh3Bot.Models;

namespace Sheesh3Bot.Functions
{
    public class ServerPublicIPRemovalFunc
    {
        // Runs once a day at 5:30 AM
        [FunctionName("ServerPublicIPRemovalFunc")]
        public static async Task Run(
            [TimerTrigger("0 30 5 * * *")]TimerInfo timer,
            ILogger log)
        {
            log.LogInformation("Scanning for turned off servers with Public IP Addresses");

            await AzureService.RemovePublicIPFromAllShutdownServers();

            log.LogInformation("Removed Public IP Addresses. TODO: Return more data about removed addresses");
        }
    }
}
