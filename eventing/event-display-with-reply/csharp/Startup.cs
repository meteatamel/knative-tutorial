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
using System.Net.Mime;
using CloudNative.CloudEvents;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace event_display_with_reply
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

            logger.LogInformation("Event Display is starting...");

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapPost("/", async context =>
                {
                    var cloudEvent = await context.Request.ReadCloudEventAsync();

                    logger.LogInformation($"Received CloudEvent\n"
                        + $"Id: {cloudEvent.Id}\n"
                        + $"Source: {cloudEvent.Source}\n"
                        + $"Type: {cloudEvent.Type}\n"
                        + $"Data: {cloudEvent.Data}");

                    var type = "dev.knative.samples.hifromknative";
                    var source = new Uri("urn:knative/eventing/samples/hello-world");
                    var newEvent = new CloudEvent(type, source)
                    {
                        DataContentType = new ContentType(MediaTypeNames.Application.Json),
                        Data = JsonConvert.SerializeObject("This is a Knative reply!")
                    };

                    logger.LogInformation($"Replying with CloudEvent\n"
                        + $"Id: {newEvent.Id}\n"
                        + $"Source: {newEvent.Source}\n"
                        + $"Type: {newEvent.Type}\n"
                        + $"Data: {newEvent.Data}");

                    //var content = new CloudEventContent(newEvent, ContentMode.Structured, new JsonEventFormatter());

                    // TODO: There must be a better way to convert CloudEvent to HTTP response
                    context.Response.ContentType = "application/json";
                    context.Response.Headers.Add("Ce-Id", newEvent.Id);
                    context.Response.Headers.Add("Ce-Specversion", "1.0");
                    context.Response.Headers.Add("Ce-Type", newEvent.Type);
                    context.Response.Headers.Add("Ce-Source", newEvent.Source.ToString());
                    await context.Response.WriteAsync(newEvent.Data.ToString());
                });
            });
        }
    }
}
