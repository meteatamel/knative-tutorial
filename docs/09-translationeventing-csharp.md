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
## Define Cloud Events

Our Knative service will receive Pub/Sub messages in the form of [CloudEvents](https://github.com/cloudevents) which roughly has the following form:

```json
{
    "ID": "",
    "Data": "",
    "Attributes": {
    },
    "PublishTime": ""
}
```
In this case, the actual translation request will be Base64 encoded in `Data` field and it's the only thing we're interested in. 

Create a [CloudEvent.cs](../eventing/translation/csharp/CloudEvent.cs) class to help us parse CloudEvents and decode `Data` field:

```csharp
using System;
using System.Collections.Generic;
using System.Text;

namespace translation
{
    public class CloudEvent
    {
        public string ID;

        public string Data;

        public Dictionary<string, string> Attributes;

        public string PublishTime;

        public string GetDecodedData() => (
            string.IsNullOrEmpty(Data) ?
                string.Empty :
                Encoding.UTF8.GetString(Convert.FromBase64String(Data)));
    }
}
```

## Add Translation API

Before adding Translation API code to our service, let's make sure Translation API is enabled in our project:

```bash
gcloud services enable translate.googleapis.com
```
And add Translation API NuGet package to our project:

```
dotnet add package Google.Cloud.Translation.V2
```
Finally, we can change [Startup.cs](../eventing/translation/csharp/Startup.cs) to first extract the `CloudEvent` and then extract `TranslationRequest` out of it.  

```csharp
var cloudEvent = JsonConvert.DeserializeObject<CloudEvent>(content);
if (cloudEvent == null) return;

var decodedData = cloudEvent.GetDecodedData();
_logger.LogInformation($"Decoded data: {decodedData}");
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

```
FROM microsoft/dotnet:2.2-sdk

WORKDIR /app
COPY *.csproj .
RUN dotnet restore

COPY . .

RUN dotnet publish -c Release -o out

ENV PORT 8080

ENV ASPNETCORE_URLS http://*:${PORT}

CMD ["dotnet", "out/translation.dll"]
```

## What's Next?
Back to [Integrate with Translation API](09-translationeventing.md)
