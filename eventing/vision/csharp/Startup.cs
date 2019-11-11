// Copyright 2019 Google LLC
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
using System.Threading.Tasks;
using Google.Cloud.Vision.V1;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace vision
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

            logger.LogInformation("Vision service is starting...");

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapPost("/", async context =>
                {
                    using (var reader = new StreamReader(context.Request.Body))
                    {
                        try
                        {
                            var content = await reader.ReadToEndAsync();
                            logger.LogInformation($"Received content: {content}");
                            
                            dynamic json = JValue.Parse(content);
                            if (json == null) return;

                            var kind = json.kind;
                            if (kind == null || kind != "storage#object") return;

                            var storageUrl = (string)ConstructStorageUrl(json);
                            logger.LogInformation($"Storage url: {storageUrl}");

                            var labels = await ExtractLabelsAsync(storageUrl);

                            var message = "This picture is labelled: " + labels;
                            logger.LogInformation(message);
                            await context.Response.WriteAsync(message);
                        }
                        catch (Exception e)
                        {
                            logger.LogError("Something went wrong: " + e.Message);
                            await context.Response.WriteAsync(e.Message);
                        }
                    }
                });
            });
        }

        private string ConstructStorageUrl(dynamic json)
        {
            return json == null? null 
                : string.Format("gs://{0}/{1}", json.bucket, json.name);
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
