using CloudLogging.Helpers;
using Google.Api;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Logging.Type;
using Google.Cloud.Logging.V2;
using Grpc.Auth;
using Grpc.Core;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Text;

namespace CloudLogging.Services
{
    public class CloudLoggingServicecs
    {
        private readonly IConfiguration _configuration;
        private readonly GoogleCredential googleCredential;
        private readonly Channel channel;
        private readonly LoggingServiceV2Client client;
        public readonly string projectId;

        public CloudLoggingServicecs(IConfiguration configuration)
        {
            _configuration = configuration;
            var helper = new CloudLoggingHelper(_configuration);

            googleCredential = helper.GetGoogleCredential();
            channel = new Channel(
                LoggingServiceV2Client.DefaultEndpoint.Host,
                LoggingServiceV2Client.DefaultEndpoint.Port,
                googleCredential.ToChannelCredentials()
                );
            client = LoggingServiceV2Client.Create(channel);
            projectId = helper.GetProjectId();
        }

        public void CreateNewLogging(string message, string logId, string type, IDictionary<string, string> entryLabels, LogSeverity severity = LogSeverity.Info)
        {
            // Prepare new log entry.
            LogEntry logEntry = new LogEntry();
            LogName logName = new LogName(projectId, logId);
            LogNameOneof logNameToWrite = LogNameOneof.From(logName);
            logEntry.LogName = logName.ToString();
            logEntry.Severity = severity;

            // Create log entry message.
            string messageId = DateTime.Now.Millisecond.ToString();
            string entrySeverity = logEntry.Severity.ToString().ToUpper();
            logEntry.TextPayload =
                $"{messageId} {entrySeverity}.Logging - {message}";

            MonitoredResource resource = new MonitoredResource
            {
                Type = type
            };

            // Add log entry to collection for writing. Multiple log entries can be added.
            IEnumerable<LogEntry> logEntries = new LogEntry[] { logEntry };

            // Write new log entry.
            client.WriteLogEntriesAsync(logNameToWrite, resource, entryLabels, logEntries);
        }

        ~CloudLoggingServicecs()
        {
            channel.ShutdownAsync().Wait();
        }
    }
}
