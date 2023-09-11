using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using System.IO;
using System.Threading.Tasks;

namespace Sheesh3Bot.Models
{
    class DiscordRestRequest
    {
        public string Signature { get; set; }
        public string Timestamp { get; set; }
        public string Body { get; set; }
        public DiscordRestRequest() { }

        public static async Task<DiscordRestRequest> CreateAsync(HttpRequest req)
        {
            var request = new DiscordRestRequest();

            request.Signature = req.Headers["x-signature-ed25519"];
            request.Timestamp = req.Headers["x-signature-timestamp"];

            var reader = new StreamReader(req.Body);
            request.Body = await reader.ReadToEndAsync();
            reader.DiscardBufferedData();
            reader.Close();

            return request;
        }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }

    }
}
