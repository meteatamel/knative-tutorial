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
using System.Threading.Tasks;
using Google.Cloud.Translation.V2;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace translation_csharp
{
    public class Startup
    {
        private readonly ILogger _logger;

        public Startup(ILogger<Startup> logger)
        {
            _logger = logger;
        }

        public void ConfigureServices(IServiceCollection services)
        {
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.Run(async (context) =>
            {
                using (var reader = new StreamReader(context.Request.Body))
                {
                    try
                    {
                        var content = reader.ReadToEnd();
                        _logger.LogInformation($"Received content: {content}");

                        var cloudEvent = JsonConvert.DeserializeObject<CloudEvent>(content);
                        if (cloudEvent == null) return;

                        var decodedData = cloudEvent.GetDecodedData();
                        _logger.LogInformation($"Decoded data: {decodedData}");
                        var translationRequest = JsonConvert.DeserializeObject<TranslationRequest>(decodedData);

                        _logger.LogInformation("Calling Translation API");
                        
                        var response = await TranslateText(translationRequest);
                        _logger.LogInformation($"Translated text: {response.TranslatedText}");
                        if (response.DetectedSourceLanguage != null) 
                        {
                            _logger.LogInformation($"Detected language: {response.DetectedSourceLanguage}");
                        }
                        await context.Response.WriteAsync(response.TranslatedText);
                    }
                    catch (Exception e)
                    {
                        _logger.LogError("Something went wrong: " + e.Message);
                        await context.Response.WriteAsync(e.Message);
                    }
                }
            });
        }

        private async Task<TranslationResult> TranslateText(TranslationRequest translationRequest)
        {
            ValidateTranslationRequest(translationRequest);

            var client = TranslationClient.Create();
            var response = await client.TranslateTextAsync(translationRequest.Text, translationRequest.To, translationRequest.From);
            return response;
        }

        private void ValidateTranslationRequest(TranslationRequest translationRequest)
        {
            if (translationRequest == null)
            {
                throw new ArgumentException("Translation request cannot be null");
            }

            if (string.IsNullOrEmpty(translationRequest.Text)) 
            {
                throw new ArgumentException("Translation text cannot be empty or null");
            }

            if (string.IsNullOrEmpty(translationRequest.To))
            {
                throw new ArgumentException("Translation 'to' cannot be empty or null");
            }
        }
    }
}
