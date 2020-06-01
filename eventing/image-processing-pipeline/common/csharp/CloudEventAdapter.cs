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
using System.Net.Mime;
using System.Threading.Tasks;
using CloudNative.CloudEvents;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Common
{
    public class CloudEventAdapter : IEventAdapter
    {
        private readonly ILogger _logger;

        public CloudEventAdapter(ILogger logger)
        {
            _logger = logger;
        }

        public async Task<CloudEvent> ReadEvent(HttpContext context)
        {
            var cloudEvent = await context.Request.ReadCloudEventAsync();
            _logger.LogInformation($"Received CloudEvent\n{cloudEvent.GetLog()}");
            return cloudEvent;
        }

        public async Task WriteEvent(string eventSource, string eventType, string eventData, HttpContext context)
        {
            var replyEvent = new CloudEvent(eventType, new Uri($"urn:{eventSource}"))
            {
                DataContentType = new ContentType("application/json"),
                Data = eventData
            };
            _logger.LogInformation("Replying with CloudEvent\n" + replyEvent.GetLog());

            // Binary format
            //TODO: There must be a better way to convert CloudEvent to HTTP response
            context.Response.Headers.Add("Ce-Id", replyEvent.Id);
            context.Response.Headers.Add("Ce-Specversion", "1.0");
            context.Response.Headers.Add("Ce-Type", replyEvent.Type);
            context.Response.Headers.Add("Ce-Source", replyEvent.Source.ToString());
            context.Response.ContentType = "application/json;charset=utf-8";
            await context.Response.WriteAsync(replyEvent.Data.ToString());
        }
    }
}