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
using System.Threading.Tasks;
using Google.Cloud.PubSub.V1;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Common
{
    public class PubSubEventWriter : IEventWriter
    {
        private readonly string _projectId;
        private readonly string[] _topicIds;
        private readonly ILogger _logger;

        public PubSubEventWriter(string projectId, string topicId, ILogger logger)
        {
            _projectId = projectId;
            _topicIds = topicId.Split(':');
            _logger = logger;
        }

        public async Task Write(string eventData, HttpContext context)
        {
            PublisherClient publisher = null;

            foreach (var topicId in _topicIds)
            {
                var topicName = new TopicName(_projectId, topicId);
                _logger.LogInformation($"Publishing to topic '{topicId}' with data '{eventData}");
                publisher = await PublisherClient.CreateAsync(topicName);
                await publisher.PublishAsync(eventData);
            }

            if (publisher != null)
            {
                await publisher.ShutdownAsync(TimeSpan.FromSeconds(10));
            }
        }
    }
}