# Integrate with Translation API

In the [previous lab](06-helloworldeventing.md), our Knative service simply logged out the received Pub/Sub event. While this might be useful for debugging, it's not terribly exciting. 

[Cloud Translation API](https://cloud.google.com/translate/docs/) is one of Machine Learning APIs of Google Cloud. It can dynamically translate text between thousands of language pairs. In this lab, we will use translation requests sent via Pub/Sub messages and use Translation API to translate text between languages. 

Since we're making calls to Google Cloud services, you need to make sure that the outbound network access is enabled, as described in the previous lab. 

## Hello World - .NET Core sample

Let's start with creating an empty ASP.NET Core app:

```bash
dotnet new web -o translation-csharp
```
Inside the `translation-csharp` folder, update [Startup.cs](../eventing/translation-csharp/Startup.cs) to log incoming messages for now:

```csharp
using System;
using System.IO;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

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

## Define translation protocol

Let's first define the translation protocol we'll use in our sample. The body of Pub/Sub messages will include text and the languages to translate from and to as follows:

```
{text = 'Hello World', from='en', to='es'}: English to Spanish
{text = 'Hello World', from='', to='es'}: Detected language to Spanish
{text = 'Hello World', from='', to=''}: Error
```
To encapsulate this, create a [TranslationRequest.cs](../eventing/translation-csharp/TranslationRequest.cs) class:

```csharp
namespace translate_csharp
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

Create a [CloudEvent.cs](../eventing/translation-csharp/CloudEvent.cs) class to help us parse CloudEvents and decode `Data` field:

```csharp
using System;
using System.Collections.Generic;
using System.Text;

namespace translation_csharp
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
Finally, we can change [Startup.cs](../eventing/translation-csharp/Startup.cs) to first extract the `CloudEvent` and then extract `TranslationRequest` out of it.  

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
You can see the full code in [Startup.cs](../eventing/translation-csharp/Startup.cs).

## Build and push Docker image

Before building the Docker image, make sure the app has no compilation errors:

```bash
dotnet build
```

Create a [Dockerfile](../eventing/translation-csharp/Dockerfile) for the image:

```
FROM microsoft/dotnet:2.2-sdk

WORKDIR /app
COPY *.csproj .
RUN dotnet restore

COPY . .

RUN dotnet publish -c Release -o out

ENV PORT 8080

ENV ASPNETCORE_URLS http://*:${PORT}

CMD ["dotnet", "out/translation-csharp.dll"]
```

Build and push the Docker image (replace `meteatamel` with your actual DockerHub): 

```docker
docker build -t meteatamel/translation-csharp:v1 .

docker push meteatamel/translation-csharp:v1
```
## Deploy the service and subscription

Create a [subscriber.yaml](../eventing/translation-csharp/subscriber.yaml) file.

```yaml
apiVersion: serving.knative.dev/v1alpha1
kind: Service
metadata:
  name: translation-csharp
spec:
  runLatest:
    configuration:
      revisionTemplate:
        spec:
          container:
            # Replace meteatamel with your actual DockerHub
            image: docker.io/meteatamel/translation-csharp:v1
---
apiVersion: eventing.knative.dev/v1alpha1
kind: Subscription
metadata:
  name: gcppubsub-source-translation-csharp
spec:
  channel:
    apiVersion: eventing.knative.dev/v1alpha1
    kind: Channel
    name: pubsub-test
  subscriber:
    ref:
      apiVersion: serving.knative.dev/v1alpha1
      kind: Service
      name: translation-csharp
```
This defines the Knative Service that will run our code and Subscription to connect to Pub/Sub messages via the Channel.

```bash
kubectl apply -f subscriber.yaml
```

Check that the service is created:

```bash
kubectl get ksvc translation-csharp
NAME            AGE
translation-csharp   8s  
```
## Test the service

We can now test our service by sending a translation request message to Pub/Sub topic:

```bash
gcloud pubsub topics publish testing --message="{text:'Hello World', from: 'en', to:'es'}"
```

Wait a little and check that a pod is created:

```bash
kubectl get pods --selector serving.knative.dev/service=translation-csharp
```
You can inspect the logs of the subscriber (replace `<podid>` with actual pod id):

```bash
kubectl logs --follow -c user-container translation-csharp-00001-deployment-<podid>
```

You should see something similar to this:

```bash
info: translation_csharp.Startup[0]
      Decoded data: {text:'Hello World', from: 'en', to:'es'}
info: translation_csharp.Startup[0]
      Calling Translation API
info: translation_csharp.Startup[0]
      Translated text: Hola Mundo
info: Microsoft.AspNetCore.Hosting.Internal.WebHost[2]
      Request finished in 767.2586ms 200 
```

## What's Next?
[Integrate with Vision API](08-visioneventing.md)
