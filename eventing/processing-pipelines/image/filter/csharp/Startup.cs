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
using Common;
using Google.Cloud.Vision.V1;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Filter
{
    public class Startup
    {
        private const string CloudEventType = "dev.knative.samples.fileuploaded";
        private const string CloudEventSource = "knative/eventing/samples/filter";

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
            var bucketExpected = configReader.Read("BUCKET", false);
            var eventWriter = configReader.ReadEventWriter();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapPost("/", async context =>
                {
                    var cloudEvent = await eventReader.Read(context);
                    var (bucket, name) = eventReader.ReadCloudStorageData(cloudEvent);

                    // This is only needed in Cloud Run (Managed) when the
                    // events are not filtered by bucket yet.
                    if (bucketExpected != null && bucket != bucketExpected)
                    {
                        logger.LogInformation($"Input bucket '{bucket}' does not match with expected bucket '{bucketExpected}'");
                        return;
                    }

                    var storageUrl = $"gs://{bucket}/{name}";
                    logger.LogInformation($"Storage url: {storageUrl}");

                    var safe = await IsPictureSafe(storageUrl);
                    logger.LogInformation($"Is the picture safe? {safe}");

                    if (!safe)
                    {
                        return;
                    }

                    var replyData = JsonConvert.SerializeObject(new {bucket = bucket, name = name});
                    await eventWriter.Write(replyData, context);
                });
            });
        }

        private async Task<bool> IsPictureSafe(string storageUrl)
        {
            var visionClient = ImageAnnotatorClient.Create();
            var response = await visionClient.DetectSafeSearchAsync(Image.FromUri(storageUrl));
            return response.Adult < Likelihood.Possible
                && response.Medical < Likelihood.Possible
                && response.Racy < Likelihood.Possible
                && response.Spoof < Likelihood.Possible
                && response.Violence < Likelihood.Possible;
        }
    }
}
