// Copyright 2020 Google LLC
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
using System;
using Microsoft.Extensions.Logging;

namespace Common
{
    public class ConfigReader
    {
        private ILogger _logger;

        public ConfigReader(ILogger logger)
        {
            _logger = logger;
        }

        public string Read(string var)
        {
            var value = Environment.GetEnvironmentVariable(var);
            CheckArgExists(value, var);
            return value;
        }

        public IEventWriter ReadEventWriter(string CloudEventSource, string CloudEventType)
        {
            var eventWriterConfig = Environment.GetEnvironmentVariable("EVENT_WRITER");
            EventWriterType eventWriterType;
            if (Enum.TryParse(eventWriterConfig, out eventWriterType))
            {
                switch (eventWriterType)
                {
                    case EventWriterType.PubSub:
                        var projectId = Environment.GetEnvironmentVariable("PROJECT_ID");
                        CheckArgExists(projectId, "PROJECT_ID");

                        var topicId = Environment.GetEnvironmentVariable("TOPIC_ID");
                        CheckArgExists(topicId, "TOPIC_ID");

                        return new PubSubEventWriter(projectId, topicId, _logger);
                }
            }
            return new CloudEventWriter(CloudEventSource, CloudEventType, _logger);
        }

        public IBucketEventDataReader ReadEventDataReader()
        {
            var eventDataReaderConfig = Environment.GetEnvironmentVariable("EVENT_DATA_READER");
            BucketDataReaderType bucketDataReaderType;
            if (Enum.TryParse(eventDataReaderConfig, out bucketDataReaderType))
            {
                switch (bucketDataReaderType)
                {
                    case BucketDataReaderType.AuditLog:
                        return new AuditLogBucketEventDataReader();
                    case BucketDataReaderType.PubSub:
                        return new PubSubBucketEventDataReader();
                }
            }
            return new CloudEventBucketEventDataReader();
        }

        private void CheckArgExists(string arg, string name)
        {
            if (string.IsNullOrEmpty(arg))
            {
                throw new ArgumentNullException(name);
            }
        }
    }
}