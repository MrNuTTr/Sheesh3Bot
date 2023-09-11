using Discord;
using Discord.Rest;
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Sheesh3Bot.Models;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Net.Http;
using Newtonsoft.Json;
using System.Text;

namespace Sheesh3Bot.Services
{
    internal class DiscordService
    {
        public static readonly HttpClient HttpClient = new HttpClient();
        public static readonly string PublicKey = Environment.GetEnvironmentVariable("DISCORD_PUBLIC_KEY");
        public static readonly DiscordRestClient Client = new DiscordRestClient(
            new DiscordRestConfig()
            {
                APIOnRestInteractionCreation = false,
                DefaultRetryMode = RetryMode.AlwaysFail,
#if DEBUG
                UseInteractionSnowflakeDate = false
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

        public static Dictionary<string, RestSlashCommandDataOption> GetSlashCommandOptionsDict(RestSlashCommandData commandData)
        {
            Dictionary<string, RestSlashCommandDataOption> options = new Dictionary<string, RestSlashCommandDataOption>();
            var optionEnumerator = commandData.Options.GetEnumerator();

            while (optionEnumerator.MoveNext())
            {
                var key = optionEnumerator.Current.Name.ToString();
                var value = optionEnumerator.Current;

                options.Add(key, value);
            }

            return options;
        }

        public static List<RestSlashCommandDataOption> GetSlashCommandOptionsList(RestSlashCommandData commandData)
        {
            List<RestSlashCommandDataOption> options = new List<RestSlashCommandDataOption>();
            var optionEnumerator = commandData.Options.GetEnumerator();
            
            while (optionEnumerator.MoveNext())
            {
                options.Add(optionEnumerator.Current);
            }

            return options;
        }

        public static async Task FollowupEditAsync(RestInteraction interaction, string content)
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

        public static async Task FollowupNewAsync(RestInteraction interaction, string content)
        {
            string baseUrl = "https://discord.com/api/webhooks";
            string url = $"{baseUrl}/{interaction.ApplicationId}/{interaction.Token}";

            var jsonData = JsonConvert.SerializeObject(new
            {
                content = content
            });

            var contentData = new StringContent(jsonData, Encoding.UTF8, "application/json");

            var response = await HttpClient.PostAsync(url, contentData);

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
