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

namespace Sheesh3Bot.Functions
{
    public class GameServerManager
    {
        [FunctionName("GameServerManager")]
        public static async Task Run([QueueTrigger("discord-gameserver-message-queue", Connection = "AzureWebJobsStorage")]string item, 
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

                if (server.ToLower() == "rlcraft")
                {
                    log.LogInformation("Turning on RLCraft Server");

                    await DiscordService.FollowupEditAsync(interaction, "Turning on the server. Please wait a couple minutes.");

                    var success = await AzureService.TurnOnGameServer(server);

                    if (success == false)
                    {
                        throw new Exception("Could not turn on server.. for some reason.");
                    }

                    string ip = await AzureService.GetServerPublicIP(server);

                    await DiscordService.FollowupNewAsync(interaction, $"Server is online. Here is the connection IP: `{ip}`" +
                        $"\nNote that it could take a few extra minutes for all the mods to load.");
                }
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

                await DiscordService.FollowupNewAsync(interaction, "Whoops, it didn't turn on. Idk man.");
            }
        }
    }
}
