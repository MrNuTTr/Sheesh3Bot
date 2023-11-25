using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Discord.Rest;
using Discord;
using System.Web.Http;
using Sheesh3Bot.Services;
using Sheesh3Bot.Models;
using Newtonsoft.Json;

namespace Sheesh3Bot.Functions
{
    public static class InteractionFunc
    {
        [FunctionName("Interactions")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest httpReq,
            [Queue("discord-openai-message-queue"), StorageAccount("AzureWebJobsStorage")] ICollector<string> openaiMessageQueue,
            [Queue("discord-gameserver-message-queue"), StorageAccount("AzureWebJobsStorage")] ICollector<string> gameServerMessageQueue,
            ILogger log)
        {
            try
            {
                DiscordRestRequest discordRequest = await DiscordRestRequest.CreateAsync(httpReq);
                var interaction = await DiscordService.ParseHttpInteractionAsync(discordRequest);

                if (interaction.Type == InteractionType.Ping)
                { 
                    log.LogInformation("Recieved Discord Ping. Responding.");
                    return new OkObjectResult(new { type = 1 });
                }

                else if (interaction.Type == InteractionType.ApplicationCommand)
                {
                    log.LogInformation("Recieved Application Command.");
                    
                    // Currently the only supported commands are slash.
                    // Will need to add proper type checking if this changes.
                    var command = (RestSlashCommand)interaction;

                    if (command.Data.Name == "support")
                    {
                        log.LogInformation("Incoming slash command: support");

                        openaiMessageQueue.Add(JsonConvert.SerializeObject(discordRequest));

                        return DiscordService.JsonResult(command.Defer());
                    }

                    if (command.Data.Name == "turn-on")
                    {
                        log.LogInformation("Incoming slash command: turn-on");

                        gameServerMessageQueue.Add(JsonConvert.SerializeObject(discordRequest));

                        return DiscordService.JsonResult(command.Defer());
                    }

                    log.LogError("Uh oh, unhandled command");
                }

                log.LogError("Unhandled interaction type.");
                return new BadRequestResult();
            }
            catch (BadSignatureException ex)
            {
                log.LogError(ex.ToString());
                return new UnauthorizedResult();
            }
            catch (Exception ex)
            {
                log.LogError(ex.ToString());
                return new ExceptionResult(ex, true);
            }
        }
    }
}
