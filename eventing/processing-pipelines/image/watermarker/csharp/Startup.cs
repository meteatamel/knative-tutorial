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
using SixLabors.Fonts;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Processing;
using Google.Cloud.Storage.V1;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Common;

// Based on https://github.com/SixLabors/Samples/blob/master/ImageSharp/DrawWaterMarkOnImage/Program.cs
namespace Watermarker
{
    public class Startup
    {
        private const string Watermark = "Google Cloud Platform";

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

            var fontCollection = new FontCollection();
            fontCollection.Install("Arial.ttf");
            var font = fontCollection.CreateFont("Arial", 10);

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
                                using (var image = Image.Load(inputStream))
                                {
                                    using (var imageProcessed = image.Clone(ctx => ApplyScalingWaterMarkSimple(ctx, font, Watermark, Color.DeepSkyBlue, 5)))
                                    {
                                        logger.LogInformation($"Added watermark to image '{name}'");
                                        imageProcessed.SaveAsJpeg(outputStream);
                                    }
                                }

                                var outputObjectName = $"{Path.GetFileNameWithoutExtension(name)}-watermark.jpeg";
                                await client.UploadObjectAsync(outputBucket, outputObjectName, "image/jpeg", outputStream);
                                logger.LogInformation($"Uploaded '{outputObjectName}' to bucket '{outputBucket}'");
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

        private static IImageProcessingContext ApplyScalingWaterMarkSimple(IImageProcessingContext processingContext,
            Font font,
            string text,
            Color color,
            float padding)
        {
            Size imgSize = processingContext.GetCurrentSize();

            float targetWidth = imgSize.Width - (padding * 2);
            float targetHeight = imgSize.Height - (padding * 2);

            // measure the text size
            FontRectangle size = TextMeasurer.Measure(text, new RendererOptions(font));

            //find out how much we need to scale the text to fill the space (up or down)
            float scalingFactor = Math.Min(imgSize.Width / size.Width, imgSize.Height / size.Height);

            //create a new font
            Font scaledFont = new Font(font, scalingFactor * font.Size);

            var center = new PointF(imgSize.Width / 2, imgSize.Height / 2);
            var textGraphicOptions = new TextGraphicsOptions()
            {
                TextOptions = {
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                }
            };
            return processingContext.DrawText(textGraphicOptions, text, scaledFont, color, center);
        }
    }
}
