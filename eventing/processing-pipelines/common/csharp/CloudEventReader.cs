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
using System.Threading.Tasks;
using CloudNative.CloudEvents;
using Google.Events;
using Google.Events.Protobuf.Cloud.Audit.V1;
using Google.Events.Protobuf.Cloud.PubSub.V1;
using Google.Events.Protobuf.Cloud.Scheduler.V1;
using Google.Events.Protobuf.Cloud.Storage.V1;
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

        private const string EVENT_TYPE_STORAGE = "google.cloud.storage.object.v1.finalized";

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
            _logger.LogInformation("Reading cloud storage data");

            string bucket = null, name = null;

            switch (cloudEvent.Type)
            {
                case EVENT_TYPE_AUDITLOG:
                    //"protoPayload" : {"resourceName":"projects/_/buckets/events-atamel-images-input/objects/atamel.jpg}";

                    var logEntryData = CloudEventConverters.ConvertCloudEventData<LogEntryData>(cloudEvent);
                    var tokens = logEntryData.ProtoPayload.ResourceName.Split('/');
                    bucket = tokens[3];
                    name = tokens[5];
                    break;
                case EVENT_TYPE_STORAGE:
                    var storageObjectData = CloudEventConverters.ConvertCloudEventData<StorageObjectData>(cloudEvent);
                    bucket = storageObjectData.Bucket;
                    name = storageObjectData.Name;
                    break;
                case EVENT_TYPE_PUBSUB:
                    // {"message": {
                    //     "data": "eyJidWNrZXQiOiJldmVudHMtYXRhbWVsLWltYWdlcy1pbnB1dCIsIm5hbWUiOiJiZWFjaC5qcGcifQ==",
                    // },"subscription": "projects/events-atamel/subscriptions/cre-europe-west1-trigger-resizer-sub-000"}

                    var messagePublishedData = CloudEventConverters.ConvertCloudEventData<MessagePublishedData>(cloudEvent);
                    var pubSubMessage = messagePublishedData.Message;
                    _logger.LogInformation($"Type: {EVENT_TYPE_PUBSUB} data: {pubSubMessage.Data.ToBase64()}");

                    var decoded = pubSubMessage.Data.ToStringUtf8();
                    _logger.LogInformation($"decoded: {decoded}");

                    var parsed = JValue.Parse(decoded);
                    bucket = (string)parsed["bucket"];
                    name = (string)parsed["name"];
                    break;
                default:
                    // Data: {"bucket":"knative-atamel-images-input","name":"beach.jpg"}
                    _logger.LogInformation($"Type: {cloudEvent.Type} data: {cloudEvent.Data}");

                    var parsedCustom = JValue.Parse((string)cloudEvent.Data);
                    bucket = (string)parsedCustom["bucket"];
                    name = (string)parsedCustom["name"];
                    break;
            }
            _logger.LogInformation($"Extracted bucket: {bucket}, name: {name}");
            return (bucket, name);
        }

        public string ReadCloudSchedulerData(CloudEvent cloudEvent)
        {
            _logger.LogInformation("Reading cloud scheduler data");

            string country = null;

            switch (cloudEvent.Type)
            {
                case EVENT_TYPE_PUBSUB:
                    var messagePublishedData = CloudEventConverters.ConvertCloudEventData<MessagePublishedData>(cloudEvent);
                    var pubSubMessage = messagePublishedData.Message;
                    _logger.LogInformation($"Type: {EVENT_TYPE_PUBSUB} data: {pubSubMessage.Data.ToBase64()}");

                    country = pubSubMessage.Data.ToStringUtf8();
                    break;
                case EVENT_TYPE_SCHEDULER:
                    // Data: {"custom_data":"Q3lwcnVz"}
                    var schedulerJobData = CloudEventConverters.ConvertCloudEventData<SchedulerJobData>(cloudEvent);
                    _logger.LogInformation($"Type: {EVENT_TYPE_SCHEDULER} data: {schedulerJobData.CustomData.ToBase64()}");

                    country = schedulerJobData.CustomData.ToStringUtf8();
                    break;
            }

            _logger.LogInformation($"Extracted country: {country}");
            return country;
        }
    }
}