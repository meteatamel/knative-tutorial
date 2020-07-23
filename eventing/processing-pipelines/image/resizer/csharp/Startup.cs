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
using Google.Cloud.Storage.V1;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp.Processing;
using Newtonsoft.Json;
using Common;

namespace Resizer
{
    public class Startup
    {
        private const string CloudEventType = "dev.knative.samples.fileresized";
        private const string CloudEventSource = "knative/eventing/samples/resizer";

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

            var eventReader = new CloudEventReader(logger);

            var configReader = new ConfigReader(logger, CloudEventSource, CloudEventType);
            var outputBucket = configReader.Read("BUCKET");
            IEventWriter eventWriter = configReader.ReadEventWriter();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapPost("/", async context =>
                {
                    try
                    {
                        var cloudEvent = await eventReader.Read(context);
                        var (bucket, name) = eventReader.ReadCloudStorageData(cloudEvent);

                        using (var inputStream = new MemoryStream())
                        {
                            var client = await StorageClient.CreateAsync();
                            await client.DownloadObjectAsync(bucket, name, inputStream);
                            logger.LogInformation($"Downloaded '{name}' from bucket '{bucket}'");

                            using (var outputStream = new MemoryStream())
                            {
                                inputStream.Position = 0; // Reset to read
                                using (Image image = Image.Load(inputStream))
                                {
                                    image.Mutate(x => x
                                        .Resize(ThumbWidth, ThumbHeight)
                                    );
                                    logger.LogInformation($"Resized image '{name}' to {ThumbWidth}x{ThumbHeight}");

                                    image.SaveAsPng(outputStream);
                                }

                                var outputObjectName = $"{Path.GetFileNameWithoutExtension(name)}-{ThumbWidth}x{ThumbHeight}.png";
                                await client.UploadObjectAsync(outputBucket, outputObjectName, "image/png", outputStream);
                                logger.LogInformation($"Uploaded '{outputObjectName}' to bucket '{outputBucket}'");

                                var replyData = JsonConvert.SerializeObject(new {bucket = outputBucket, name = outputObjectName});
                                await eventWriter.Write(replyData, context);
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        logger.LogError($"Error processing: " + e.Message);
                        throw e;
                    }
                });
            });
        }
    }
}
