# Create Vision Handler - C#

## Create Vision Handler

Let's start with creating an empty ASP.NET Core app:

```bash
dotnet new web -o vision
```

Inside the `vision/csharp` folder, update [Startup.cs](../eventing/vision/csharp/Startup.cs) to log incoming messages for now:

```csharp
using System;
using System.IO;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace vision
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

                        await context.Response.WriteAsync(content);
                    }
                    catch (Exception e)
                    {
                        _logger.LogError("Something went wrong: " + e.Message);
                        await context.Response.WriteAsync(e.Message);
                    }
                }
            });
        }
    }
}
```

Change the log level in `appsettings.json` to `Information`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information"
    }
  },
  "AllowedHosts": "*"
}
```

At this point, the sample simply logs out the received messages.

## Add Vision API

Add Vision API NuGet package to our project:

```bash
dotnet add package Google.Cloud.Vision.V1
```

We can now update [Startup.cs](../eventing/vision/csharp/Startup.cs) to check for `storage#object` events. These events are emitted by Cloud Storage when a file is uploaded.  

```csharp
dynamic json = JValue.Parse(content);
if (json == null) return;

var kind = json.kind;
if (kind == null || kind != "storage#object") return;
```

Next, we extract the Cloud Storage URL of the file from the event:

```csharp
var storageUrl = (string)ConstructStorageUrl(json);

private string ConstructStorageUrl(dynamic json)
{
    return json == null? null 
        : string.Format("gs://{0}/{1}", json.bucket, json.name);
}
```

Finally, make a call to Vision API to extract labels from the image:

```csharp
var labels = await ExtractLabelsAsync(storageUrl);

var message = "This picture is labelled: " + labels;
_logger.LogInformation(message);
await context.Response.WriteAsync(message);

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
```

You can see the full code in [Startup.cs](../eventing/vision/csharp/Startup.cs).

Before building the Docker image, make sure the app has no compilation errors:

```bash
dotnet build
```

## Create Dockerfile

Create a [Dockerfile](../eventing/vision/csharp/Dockerfile) for the image:

```dockerfile
FROM mcr.microsoft.com/dotnet/core/sdk:2.2-alpine AS build
WORKDIR /app

# Install production dependencies.
# Copy csproj and restore as distinct layers.
COPY *.csproj ./
RUN dotnet restore

# Copy local code to the container image.
COPY . ./
WORKDIR /app

# Build a release artifact.
RUN dotnet publish -c Release -o out

# Use Microsoft's official runtime .NET image.
# https://hub.docker.com/_/microsoft-dotnet-core-aspnet/
# TODO: aspnet:2.2-alpine does not work for some reason
# FROM mcr.microsoft.com/dotnet/core/aspnet:2.2 AS runtime
FROM mcr.microsoft.com/dotnet/core/aspnet:2.2 AS runtime
WORKDIR /app
COPY --from=build /app/out ./

# Run the web service on container startup.
ENTRYPOINT ["dotnet", "vision.dll"]
```

## What's Next?

Back to [Integrate with Vision API](10-visioneventing.md)
