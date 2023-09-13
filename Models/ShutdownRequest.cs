using Azure;
using Azure.Data.Tables;
using System;

namespace Sheesh3Bot.Models
{
    public class ShutdownRequest : ITableEntity
    {
        public string ServerName { get; set; }
        // In UTC format
        public DateTime ScheduledShutdownTime { get; set; }

        // Default attributes
        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }
    }
}
