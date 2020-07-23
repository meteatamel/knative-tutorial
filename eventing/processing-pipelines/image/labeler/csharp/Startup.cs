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
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common;
using Google.Cloud.Storage.V1;
using Google.Cloud.Vision.V1;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace Labeler
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

            var eventReader = new CloudEventReader(logger);

            var configReader = new ConfigReader(logger);
            var outputBucket = configReader.Read("BUCKET");

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapPost("/", async context =>
                {
                    try
                    {
                        var cloudEvent = await eventReader.Read(context);
                        var (bucket, name) = eventReader.ReadCloudStorageData(cloudEvent);

                        var storageUrl = $"gs://{bucket}/{name}";
                        logger.LogInformation($"Storage url: {storageUrl}");

                        var labels = await ExtractLabelsAsync(storageUrl);
                        logger.LogInformation($"This picture is labelled: {labels}");

                        using (var outputStream = new MemoryStream(Encoding.UTF8.GetBytes(labels)))
                        {
                            var outputObjectName = $"{Path.GetFileNameWithoutExtension(name)}-labels.txt";
                            var client = await StorageClient.CreateAsync();
                            await client.UploadObjectAsync(outputBucket, outputObjectName, "text/plain", outputStream);
                            logger.LogInformation($"Uploaded '{outputObjectName}' to bucket '{outputBucket}'");
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

        private async Task<string> ExtractLabelsAsync(string storageUrl)
        {
            var visionClient = ImageAnnotatorClient.Create();
            var labels = await visionClient.DetectLabelsAsync(Image.FromUri(storageUrl), maxResults: 10);

            var orderedLabels = labels
                .OrderByDescending(x => x.Score)
                .TakeWhile((x, i) => i <= 2 || x.Score > 0.50)
                .Select(x => x.Description)
                .ToList();

            return string.Join(",", orderedLabels.ToArray());
        }
    }
}