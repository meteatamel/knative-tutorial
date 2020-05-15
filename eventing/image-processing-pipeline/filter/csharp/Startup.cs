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
using Google.Cloud.Vision.V1;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Filter
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILogger<Startup> logger)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            logger.LogInformation("Service is starting...");

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapPost("/", async context =>
                {
                    var cloudEvent = await context.Request.ReadCloudEventAsync();
                    logger.LogInformation("Received CloudEvent\n" + GetEventLog(cloudEvent));

                    dynamic data = JValue.Parse((string)cloudEvent.Data);
                    var storageUrl = ConstructStorageUrl(data);
                    logger.LogInformation($"Storage url: {storageUrl}");

                    var safe = await DetectSafeSearch(storageUrl);
                    logger.LogInformation($"Is the picture safe? {safe}");

                    if (!safe) {
                        return;
                    }

                    var replyEvent = GetEventReply(cloudEvent.Data);
                    logger.LogInformation("Replying with CloudEvent\n" + GetEventLog(replyEvent));

                    // Binary format
                    //TODO: There must be a better way to convert CloudEvent to HTTP response
                    context.Response.Headers.Add("Ce-Id", replyEvent.Id);
                    context.Response.Headers.Add("Ce-Specversion", "1.0");
                    context.Response.Headers.Add("Ce-Type", replyEvent.Type);
                    context.Response.Headers.Add("Ce-Source", replyEvent.Source.ToString());
                    context.Response.ContentType = "application/json;charset=utf-8";
                    await context.Response.WriteAsync(replyEvent.Data.ToString());
                });
            });
        }

        private string ConstructStorageUrl(dynamic data)
        {
            return data == null? null
                : string.Format("gs://{0}/{1}", data.bucket, data.name);
        }

        private async Task<bool> DetectSafeSearch(string storageUrl)
        {
            var visionClient = ImageAnnotatorClient.Create();
            var response = await visionClient.DetectSafeSearchAsync(Image.FromUri(storageUrl));
            return response.Adult < Likelihood.Possible
                || response.Medical < Likelihood.Possible
                || response.Racy < Likelihood.Possible
                || response.Spoof < Likelihood.Possible
                || response.Violence < Likelihood.Possible;
        }

        private CloudEvent GetEventReply(object data)
        {
            var type = "dev.knative.samples.fileuploaded";
            var source = new Uri("urn:knative/eventing/samples/filter");
            var replyEvent = new CloudEvent(type, source)
            {
                DataContentType = new ContentType(MediaTypeNames.Application.Json),
                Data = data
            };
            return replyEvent;
        }
        private string GetEventLog(CloudEvent cloudEvent)
        {
            return $"ID: {cloudEvent.Id}\n"
                + $"Source: {cloudEvent.Source}\n"
                + $"Type: {cloudEvent.Type}\n"
                + $"Subject: {cloudEvent.Subject}\n"
                + $"DataSchema: {cloudEvent.DataSchema}\n"
                + $"DataContentType: {cloudEvent.DataContentType}\n"
                + $"Time: {cloudEvent.Time?.ToUniversalTime():yyyy-MM-dd'T'HH:mm:ss.fff'Z'}\n"
                + $"SpecVersion: {cloudEvent.SpecVersion}\n"
                + $"Data: {cloudEvent.Data}";
        }
    }
}
