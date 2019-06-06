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

## Define Cloud Events

Our Knative service will receive Pub/Sub messages from Cloud Storage in the form of [CloudEvents](https://github.com/cloudevents) which roughly has the following form:

```json
{
    "ID": "",
    "Data": "",
    "Attributes": {
    },
    "PublishTime": ""
}
```
In this case, the `Attributes` has the bucket and file information that we're interested in.

Create a [CloudEvent.cs](../eventing/vision/csharp/CloudEvent.cs):

```csharp
using System;
using System.Collections.Generic;
using System.Text;

namespace vision
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

## Add Vision API

Before adding Translation API code to our service, let's make sure Translation API is enabled in our project:

```bash
gcloud services enable vision.googleapis.com
```
And add Vision API NuGet package to our project:

```
dotnet add package Google.Cloud.Vision.V1
```
We can now update [Startup.cs](../eventing/vision/csharp/Startup.cs) to first extract the `CloudEvent` and then check for `OBJECT_FINALIZE` events. These events are emitted by Cloud Storage when a file is uploaded.  

```csharp
var cloudEvent = JsonConvert.DeserializeObject<CloudEvent>(content);
if (cloudEvent == null) return;

var eventType = cloudEvent.Attributes["eventType"];
if (eventType == null || eventType != "OBJECT_FINALIZE") return;
```

Next, we extract the Cloud Storage URL of the file from the event:

```csharp
var storageUrl = ConstructStorageUrl(cloudEvent);

private string ConstructStorageUrl(CloudEvent cloudEvent)
{
    return cloudEvent == null? null 
        : string.Format("gs://{0}/{1}", cloudEvent.Attributes["bucketId"], cloudEvent.Attributes["objectId"]);
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

```
FROM microsoft/dotnet:2.2-sdk

WORKDIR /app
COPY *.csproj .
RUN dotnet restore

COPY . .

RUN dotnet publish -c Release -o out

ENV PORT 8080

ENV ASPNETCORE_URLS http://*:${PORT}

CMD ["dotnet", "out/vision.dll"]
```

## What's Next?
Back to [Integrate with Vision API](10-visioneventing.md)
