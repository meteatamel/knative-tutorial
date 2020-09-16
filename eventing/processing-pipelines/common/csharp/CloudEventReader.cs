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
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace Common
{
    public class CloudEventReader
    {
        private const string EVENT_TYPE_AUDITLOG = "google.cloud.audit.log.v1.written";
        private const string EVENT_TYPE_PUBSUB = "google.cloud.pubsub.topic.v1.messagePublished";

        private const string EVENT_TYPE_SCHEDULER = "google.cloud.scheduler.job.v1.executed";

        private readonly ILogger _logger;

        public CloudEventReader(ILogger logger)
        {
            _logger = logger;
        }

        public async Task<CloudEvent> Read(HttpContext context)
        {
            var cloudEvent = await context.Request.ReadCloudEventAsync();
            _logger.LogInformation($"Received CloudEvent\n{cloudEvent.GetLog()}");
            return cloudEvent;
        }

        public (string, string) ReadCloudStorageData(CloudEvent cloudEvent)
        {
            _logger.LogInformation($"Parsing cloud storage data for type: {cloudEvent.Type}");

            dynamic cloudEventData = JValue.Parse((string)cloudEvent.Data);

            switch (cloudEvent.Type)
            {
                case EVENT_TYPE_AUDITLOG:
                    //"protoPayload" : {"resourceName":"projects/_/buckets/events-atamel-images-input/objects/atamel.jpg}";
                    var tokens = ((string)cloudEventData.protoPayload.resourceName).Split('/');
                    var bucket = tokens[3];
                    var name = tokens[5];
                    return (bucket, name);
                case EVENT_TYPE_PUBSUB:
                    // {"message": {
                    //     "data": "eyJidWNrZXQiOiJldmVudHMtYXRhbWVsLWltYWdlcy1pbnB1dCIsIm5hbWUiOiJiZWFjaC5qcGcifQ==",
                    // },"subscription": "projects/events-atamel/subscriptions/cre-europe-west1-trigger-resizer-sub-000"}
                    var data = (string)cloudEventData["message"]["data"];
                    _logger.LogInformation($"data: {data}");

                    var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(data));
                    _logger.LogInformation($"decoded: {decoded}");

                    var parsed = JValue.Parse(decoded);
                    return ((string)parsed["bucket"], (string)parsed["name"]);
            }
            // google.cloud.storage.object.v1.finalized
            return (cloudEventData.bucket, cloudEventData.name);
        }

        public string ReadCloudSchedulerData(CloudEvent cloudEvent)
        {
            switch (cloudEvent.Type)
            {
                case EVENT_TYPE_PUBSUB:
                    var cloudEventData = JValue.Parse((string)cloudEvent.Data);
                    var data = (string)cloudEventData["message"]["data"];
                    var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(data));
                    return decoded;
                case EVENT_TYPE_SCHEDULER:
                    return (string)cloudEvent.Data;
            }

            //Data: {"custom_data":"Q3lwcnVz"}
            var parsed = JValue.Parse((string)cloudEvent.Data);
            var customData = (string)parsed["custom_data"];
            var country = Encoding.UTF8.GetString(Convert.FromBase64String(customData));
            return country;
        }
    }
}