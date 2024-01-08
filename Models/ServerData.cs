using Azure;
using Azure.Data.Tables;
using System;

namespace Sheesh3Bot.Models
{
    public class ServerData : ITableEntity
    {
        public string ResourceID { get; set; }

        // Default attributes
        /// <summary>
        /// Acts as ServerID / ServerName
        /// </summary>
        public string PartitionKey { get; set; }
        /// <summary>
        /// Acts as ServerID / ServerName
        /// </summary>
        public string RowKey { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }
    }
}
