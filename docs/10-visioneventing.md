# Integrate with Vision API

[Cloud Vision API](https://cloud.google.com/vision/docs) is another Machine Learning API of Google Cloud. You can use it to derive insight from your images with powerful pre-trained API models or easily train custom vision models with AutoML Vision.

In this lab, we will use a [Cloud Storage](https://cloud.google.com/storage/docs/) bucket to store our images. We will also enable [Pub/Sub notifications](https://cloud.google.com/storage/docs/pubsub-notifications) on our bucket. This way, every time we add an image to the bucket, it will trigger a Pub/Sub message. This in turn will trigger our Knative service where we will use Vision API to analyze the image. Pretty cool!

Since we're making calls to Google Cloud services, you need to make sure that the outbound network access is enabled, as described [here](https://github.com/knative/docs/blob/master/serving/outbound-network-access.md). 

## Hello World - .NET Core sample

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

## Build and push Docker image

Before building the Docker image, make sure the app has no compilation errors:

```bash
dotnet build
```

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

Build and push the Docker image (replace `{username}` with your actual DockerHub): 

```docker
docker build -t {username}/vision:v1 .

docker push {username}/vision:v1
```
## Deploy the service and trigger

Create a [trigger.yaml](../eventing/vision/trigger.yaml) file.

```yaml
# limitations under the License.
apiVersion: serving.knative.dev/v1alpha1
kind: Service
metadata:
  name: vision
spec:
  runLatest:
    configuration:
      revisionTemplate:
        spec:
          container:
            # Replace {username} with your actual DockerHub
            image: docker.io/{username}/vision:v1
        metadata:
          annotations:
            # Disable scale to zero with a minScale of 1.
            autoscaling.knative.dev/minScale: "1"
---
apiVersion: eventing.knative.dev/v1alpha1
kind: Trigger
metadata:
  name: vision
spec:
  subscriber:
    ref:
      apiVersion: serving.knative.dev/v1alpha1
      kind: Service
      name: vision
```
This defines the Knative Service that will run our code and Trigger to connect to Pub/Sub messages.

```bash
kubectl apply -f trigger.yaml
```

Check that the service and trigger are created:

```bash
kubectl get ksvc,trigger
```
## Create bucket and enabled notifications

Before we can test the service, let's first create a Cloud Storage bucket. You can do this [in many ways](https://cloud.google.com/storage/docs/creating-buckets). We'll use `gsutil` as follows (replace `knative-bucket` with a unique name):

```bash
gsutil mb gs://knative-bucket/
```
Once the bucket is created, enable Pub/Sub notifications on it and link to our `testing` topic we created in earlier labs:

```bash
gsutil notification create -t testing -f json gs://knative-bucket/
```
Check that the notification is created:

```bash
gsutil notification list gs://knative-bucket
```
## Test the service

We can finally test our Knative service by uploading an image to the bucket. 

First, let's watch the logs of the service. Wait a little and check that a pod is created:

```bash
kubectl get pods --selector serving.knative.dev/service=vision
```
You can inspect the logs of the subscriber:

You can inspect the logs of the subscriber (replace `<podid>` with actual pod id):

```bash
kubectl logs --follow -c user-container <podid>
```

Drop the image to the bucket in Google Cloud Console or use `gsutil` to copy the file as follows:

```bash
gsutil cp beach.jpg gs://knative-bucket/
```
This triggers a Pub/Sub message to our Knative service. 

You should see something similar to this in logs:

```bash
info: vision.Startup[0]
      This picture is labelled: Sky,Body of water,Sea,Nature,Coast,Water,Sunset,Horizon,Cloud,Shore
info: Microsoft.AspNetCore.Hosting.Internal.WebHost[2]
      Request finished in 1948.3204ms 200 
```

## What's Next?
[Hello World Build](11-helloworldbuild.md)
