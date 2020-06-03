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
using System.Text;
using System.Threading.Tasks;
using CloudNative.CloudEvents;
using Google.Cloud.PubSub.V1;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace Common
{
    public class PubSubEventWriter : IEventWriter
    {
        private readonly string _projectId;
        private readonly string _topicId;
        private readonly ILogger _logger;

        public PubSubEventWriter(string projectId, string topicId, ILogger logger)
        {
            _projectId = projectId;
            _topicId = topicId;
            _logger = logger;
        }

        public async Task Write(string eventData, HttpContext context)
        {
            var topicName = new TopicName(_projectId, _topicId);
            _logger.LogInformation($"Publishing to topic '{_topicId}' with data '{eventData}");
            var publisher = await PublisherClient.CreateAsync(topicName);
            await publisher.PublishAsync(eventData);
            await publisher.ShutdownAsync(TimeSpan.FromSeconds(10));
        }
    }
}