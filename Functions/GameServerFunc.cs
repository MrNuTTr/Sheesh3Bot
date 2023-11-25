using Discord.Rest;
using Discord;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Sheesh3Bot.Models;
using Sheesh3Bot.Services;
using System.Threading.Tasks;
using System;
using System.Net.Http;
using Azure.Data.Tables;

namespace Sheesh3Bot.Functions
{
    public class GameServerFunc
    {
        [FunctionName("GameServerFunc")]
        public static async Task Run(
            [QueueTrigger("discord-gameserver-message-queue", Connection = "AzureWebJobsStorage")] string item,
            [Table("serverTimerShutdown")] TableClient tableClient,
            ILogger log)
        {
            //Parse message
            var discordRequest = JsonConvert.DeserializeObject<DiscordRestRequest>(item);
            var interaction = await DiscordService.ParseHttpInteractionAsync(discordRequest);
            var command = (RestSlashCommand)interaction;
            var options = DiscordService.GetSlashCommandOptionsDict(command.Data);

            try
            {
                var server = options["server"].Value.ToString();

                log.LogInformation($"Turning on {server} Server");

                await DiscordService.FollowupEditAsync(interaction, "Turning on the server. Please wait a minute.");

                AzureService.SendServerShutdownRequest(tableClient, server, DateTime.UtcNow.AddHours(3));

                var success = await AzureService.TurnOnGameServer(server);
                string ip = await AzureService.GetServerPublicIP(server);
                string msg = "";

                if (success != 0 && string.IsNullOrEmpty(ip))
                {
                    ip = await AzureService.CreateServerPublicIP(server);
                }

                if (success == 0)
                {
                    msg = $"Couldn't turn on server {server}. Probably doesn't exist.";
                    log.LogError(msg);
                }
                else if (success == 1)
                {
                    msg = $"Server is online with IP: `{ip}`" +
                        $"\nServer will turn off in 3 hours to save me *M0n3y$*." +
                        $"\n";
                }
                else
                {
                    msg = $"Bro it's already turned on. Here's the IP: `{ip}`";
                }

                await DiscordService.FollowupNewAsync(interaction, msg);
            }
            catch (HttpRequestException ex)
            {
                log.LogError(ex.Message);
                log.LogError(ex.StackTrace);
            }
            catch (Exception ex)
            {
                log.LogError(ex.Message);
                log.LogError(ex.StackTrace);

                await DiscordService.FollowupNewAsync(interaction, 
                    $"Whoops, that didn't work for some reason. Get someone to check the logs.");
            }
        }
    }
}
