using Discord;
using Discord.Rest;
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Sheesh3Bot.Models;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Net.Http;
using static System.Net.WebRequestMethods;
using Newtonsoft.Json;
using System.Text;

namespace Sheesh3Bot
{
    internal class Discord
    {
        public static readonly HttpClient HttpClient = new HttpClient();
        public static readonly string PublicKey = Environment.GetEnvironmentVariable("DISCORD_PUBLIC_KEY");
        public static readonly DiscordRestClient Client = new DiscordRestClient(
            new DiscordRestConfig()
            {
                APIOnRestInteractionCreation = false,
                DefaultRetryMode = RetryMode.AlwaysFail,
#if DEBUG
                UseInteractionSnowflakeDate = false,
#endif
            });

        public static async Task<RestInteraction> ParseHttpInteractionAsync(DiscordRestRequest req)
        {
            return await Client.ParseHttpInteractionAsync(PublicKey, req.Signature, req.Timestamp, req.Body);
        }

        public static async Task<RestInteraction> ParseHttpInteractionAsync(HttpRequest req)
        {
            DiscordRestRequest discordRest = await DiscordRestRequest.CreateAsync(req);

            return await ParseHttpInteractionAsync(discordRest);
        }

        public static List<RestSlashCommandDataOption> GetDataOptions(RestSlashCommandData commandData)
        {
            List<RestSlashCommandDataOption> options = new List<RestSlashCommandDataOption>();
            var optionEnumerator = commandData.Options.GetEnumerator();
            
            while (optionEnumerator.MoveNext())
            {
                options.Add(optionEnumerator.Current);
            }

            return options;
        }

        public static async Task FollowupAsync(RestInteraction interaction, string content)
        {
            string baseUrl = "https://discord.com/api/webhooks";
            string url = $"{baseUrl}/{interaction.ApplicationId}/{interaction.Token}/messages/@original";

            var jsonData = JsonConvert.SerializeObject(new
            {
                content = content
            });

            var contentData = new StringContent(jsonData, Encoding.UTF8, "application/json");

            var response = await HttpClient.PatchAsync(url, contentData);

            response.EnsureSuccessStatusCode();
        }

        public static ContentResult JsonResult(string content)
        {
            return new ContentResult
            {
                Content = content,
                ContentType = "application/json"
            };
        }
    }
}
