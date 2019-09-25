# Create Translation Handler - C#

## Create Translation Handler

Let's start with creating an empty ASP.NET Core app:

```bash
dotnet new web -o translation
```

Inside the `translation/csharp` folder, update [Startup.cs](../eventing/translation/csharp/Startup.cs) to log incoming messages for now:

```csharp
using System;
using System.IO;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace translation
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

## Handle translation protocol

To encapsulate translation protocol, create a [TranslationRequest.cs](../eventing/translation/csharp/TranslationRequest.cs) class:

```csharp
namespace translate
{
    public class TranslationRequest
    {
        public string From;

        public string To;

        public string Text;
    }
}
```

## Add Translation API

Add Translation API NuGet package to our project:

```bash
dotnet add package Google.Cloud.Translation.V2
```

Change [Startup.cs](../eventing/translation/csharp/Startup.cs) to extract `TranslationRequest` out of it.  

```csharp
var translationRequest = JsonConvert.DeserializeObject<TranslationRequest>(decodedData);
```

Once we have the translation request, we can pass that to Translation API:

```csharp
_logger.LogInformation("Calling Translation API");

var response = await TranslateText(translationRequest);
_logger.LogInformation($"Translated text: {response.TranslatedText}");
if (response.DetectedSourceLanguage != null)
{
    _logger.LogInformation($"Detected language: {response.DetectedSourceLanguage}");
}
await context.Response.WriteAsync(response.TranslatedText);
```

You can see the full code in [Startup.cs](../eventing/translation/csharp/Startup.cs).

Before building the Docker image, make sure the app has no compilation errors:

```bash
dotnet build
```

## Create Dockerfile

Create a [Dockerfile](../eventing/translation/csharp/Dockerfile) for the image:

```dockerfile
# Use Microsoft's official lightweight build .NET image.
# https://hub.docker.com/_/microsoft-dotnet-core-sdk/
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
FROM mcr.microsoft.com/dotnet/core/aspnet:2.2-alpine AS runtime
WORKDIR /app
COPY --from=build /app/out ./

# Run the web service on container startup.
ENTRYPOINT ["dotnet", "translation.dll"]
```

## What's Next?

Back to [Integrate with Translation API](09-translationeventing.md)