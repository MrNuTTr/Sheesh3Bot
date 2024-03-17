using System;
using System.Threading.Tasks;
using Azure.Data.Tables;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Sheesh3Bot.Services;
using Sheesh3Bot.Models;
using System.Threading;


namespace Sheesh3Bot.Functions
{
    public class ServerTimerShutdownFunc
    {
        [FunctionName("ServerTimerShutdownFunc")]
        public static async Task Run(
            [TimerTrigger("0 */10 * * * *")]TimerInfo timer,
            [Table("serverTimerShutdownTable")] TableClient tableClient,
            [Table("serverDataTable")] TableClient serverDataTable,
            ILogger log)
        {
            log.LogInformation("Processing server shutdown requests");
            var queryResults = tableClient.QueryAsync<ShutdownRequest>();

            int serversTurnedOff = 0;
            await foreach(var request in queryResults)
            {
                if (request.ScheduledShutdownTime >= DateTime.UtcNow)
                {
                    continue;
                }

                var resourceId = serverDataTable.GetEntity<ServerData>(request.ServerName, request.ServerName).Value.ResourceID;
                var averageNetworkBytes = AzureService.GetAverageNetworkUsageBytesPast10Minutes(resourceId);

                log.LogInformation($"{request.ServerName} has {averageNetworkBytes} average bytes of activity in the last 10 minutes");

                if (averageNetworkBytes < 100000.0)
                {
                    log.LogInformation($"Turning off server: {request.ServerName}");
                    await AzureService.TurnOffGameServer(resourceId);

                    log.LogInformation("Attempting to delete record");
                    await tableClient.DeleteEntityAsync(request.PartitionKey, request.RowKey);

                    serversTurnedOff++;
                }
            }

            log.LogInformation("Finished processing server shtudown requests");
            if (serversTurnedOff == 0)
            {
                log.LogInformation("No servers were shut down");
            }
            else
            {
                log.LogInformation($"A total of {serversTurnedOff} servers were shut down");
            }
        }
    }
}
