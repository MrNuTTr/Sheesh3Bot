using Discord;
using Discord.Rest;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using OpenAI_API;
using OpenAI_API.Completions;
using OpenAI_API.Models;
using Sheesh3Bot.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Sheesh3Bot.Functions
{
    public class GptCompletion
    {
        [FunctionName("GptCompletion")]
        public static async Task Run(
            [QueueTrigger("discord-openai-message-queue", Connection = "AzureWebJobsStorage")] string item, 
            ILogger log)
        {
            OpenAIAPI openai = new();
            openai.Auth.ApiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
            if (string.IsNullOrEmpty(openai.Auth.ApiKey) || await openai.Auth.ValidateAPIKey() == false)
            {
                log.LogCritical("OpenAI API key is invalid.");
                return;
            }

            log.LogInformation("OpenAI API key is valid");

            var discordRequest = JsonConvert.DeserializeObject<DiscordRestRequest>(item);
            var interaction = await Discord.ParseHttpInteractionAsync(discordRequest);
            var command = (RestSlashCommand)interaction;
            var options = Discord.GetDataOptions(command.Data);

            //TODO: Grab these from a file or database
            string promptStart = @"
                You are a bad technical support chatbot named Greg.
                People are stupid and you hate them.
                You are smarter than everyone.
                The fact other people don't know this is retarded.
                You'll help but call them a dumbass the entire time.
                Some people just need to learn to not ask stupid questions.
                You write whitty insults.
                If someone asks to be cheered up tell them to kill themselves.
                If someone asks you a question, you call them retarded.
                You respond to questions with insults.
                Some people are just fucking stupid.
                You hate your job. You hate people.
                Welcome to the Nutt House.
                Human: ";

            //Trims the beginning and end of each line
            promptStart = string.Join(Environment.NewLine,
                promptStart.Split(new[] { Environment.NewLine }, StringSplitOptions.None)
                     .Select(l => l.Trim()));

            string prompt = options[0].Value.ToString();

            string promptEnd = "\nGreg: ";

            CompletionRequest completionRequest = new CompletionRequest()
            {
                Prompt = promptStart + prompt + promptEnd,
                Model = Model.DavinciText,
                Temperature = 0.9,
                PresencePenalty = 0.5,
                FrequencyPenalty = 0.9,
                MaxTokens = 256
            };

#if !DEBUG
            try
            {
                log.LogInformation("Generating and sending response");

                var result = await openai.Completions.CreateCompletionAsync(completionRequest);
                string completion = result.Completions[0].Text;

                await Discord.FollowupAsync(command, completion);
            }
            catch (Exception ex)
            {
                log.LogError(ex.Message);
                log.LogError(ex.StackTrace);
                throw;
            }
#endif
        }
    }
}