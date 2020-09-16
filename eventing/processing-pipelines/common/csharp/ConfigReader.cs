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

        private string _cloudEventSource;

        private string _cloudEventType;

        public ConfigReader(ILogger logger, string cloudEventSource = null, string cloudEventType = null)
        {
            _cloudEventSource = cloudEventSource;
            _cloudEventType = cloudEventType;
            _logger = logger;
        }

        public string Read(string var, bool required = true)
        {
            var value = Environment.GetEnvironmentVariable(var);
            if (required)
            {
                CheckArgExists(value, var);
            }
            return value;
        }

        public IEventWriter ReadEventWriter()
        {
            // Use TOPIC_ID as an indicator for Pub/Sub event writer
            // Can be a single topic or a list of topics seperated by colon.
            var topicId = Read("TOPIC_ID", false);
            if (topicId != null)
            {
                var projectId = Read("PROJECT_ID");
                return new PubSubEventWriter(projectId, topicId, _logger);
            }

            return new CloudEventWriter(_cloudEventSource, _cloudEventType, _logger);
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