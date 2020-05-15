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
using System.IO;
using SixLabors.ImageSharp;
using CloudNative.CloudEvents;
using Google.Cloud.Storage.V1;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using SixLabors.ImageSharp.Processing;
using System.Net.Mime;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;

namespace Resizer
{
    public class Startup
    {
        private const string EventType = "dev.knative.samples.fileresized";
        private const string EventSource = "knative/eventing/samples/resizer";

        private const int ThumbWidth = 400;
        private const int ThumbHeight = 400;

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
                    var inputBucket = (string)data.bucket;
                    var inputObjectName = (string)data.name;

                    try
                    {
                        using (var inputStream = new MemoryStream())
                        {
                            var client = await StorageClient.CreateAsync();
                            await client.DownloadObjectAsync(inputBucket, inputObjectName, inputStream);
                            logger.LogInformation($"Downloaded '{inputObjectName}' from bucket '{inputBucket}'");

                            using (var outputStream = new MemoryStream())
                            {
                                inputStream.Position = 0; // Reset to read
                                using (Image image = Image.Load(inputStream))
                                {
                                    image.Mutate(x => x
                                        .Resize(ThumbWidth, ThumbHeight)
                                    );
                                    logger.LogInformation($"Resized image '{inputObjectName}' to {ThumbWidth}x{ThumbHeight}");

                                    image.SaveAsPng(outputStream);
                                }

                                var outputBucket = Environment.GetEnvironmentVariable("BUCKET");
                                var outputObjectName = $"{Path.GetFileNameWithoutExtension(inputObjectName)}-{ThumbWidth}x{ThumbHeight}.png";
                                await client.UploadObjectAsync(outputBucket, outputObjectName, "image/png", outputStream);
                                logger.LogInformation($"Uploaded '{outputObjectName}' to bucket '{outputBucket}'");

                                var replyData = JsonConvert.SerializeObject(new {bucket = outputBucket, name = outputObjectName});
                                var replyEvent = GetEventReply(replyData);
                                logger.LogInformation("Replying with CloudEvent\n" + GetEventLog(replyEvent));

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
                    catch (Exception e)
                    {
                        logger.LogError($"Error processing {inputObjectName}: " + e.Message);
                        throw e;
                    }
                });
            });
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

        private CloudEvent GetEventReply(object data)
        {
            var replyEvent = new CloudEvent(EventType, new Uri($"urn:{EventSource}"))
            {
                DataContentType = new ContentType(MediaTypeNames.Application.Json),
                Data = data
            };
            return replyEvent;
        }
    }
}
