# Hello World Eventing

You probably installed [Knative Eventing](https://github.com/knative/docs/tree/master/eventing) when you [installed Knative](https://github.com/knative/docs/blob/master/install/Knative-with-GKE.md#installing-knative). If not, Knative Eventing has an [Installation](https://github.com/knative/docs/tree/master/eventing#installation) section. We are assuming that you already went through it.

Knative Eventing has a few different types of event sources (Kubernetes, GitHub, GCP Pub/Sub etc.) and supports direct/simple and fanout delivery options:

![Diagram](https://github.com/knative/docs/blob/master/eventing/control-plane.png?raw=true)

In this tutorial, we will focus on GCP Pub/Sub events and fanout delivery using Channel and Subscription. Setup Knative Eventing using the `release-with-gcppubsub.yaml` file as described in the [GCP Cloud Pub/Sub](https://github.com/knative/docs/tree/master/eventing/samples/gcp-pubsub-source) page.

## Configuring outbound network access

In Knative, the outbound network access is disabled by default. This means that you cannot even call Google Cloud APIs from Knative. 

In our samples, we want to call Google Cloud APIs, so make sure you follow instructions on [Configuring outbound network access](https://github.com/knative/docs/blob/master/serving/outbound-network-access.md) page to enable access. 

## Setup GcpPubSubSource and channel

[GCP Cloud Pub/Sub](https://github.com/knative/docs/tree/master/eventing/samples/gcp-pubsub-source) page shows how to configure a GCP Pub/Sub event source. A `Go` Knative service listens for Pub/Sub events and dumps the contents of the event message. Go through this tutorial to setup eventing infrastructure. 

At the end of the tutorial, make sure you have the `gcppubsubsource` and `channel` are setup:

```bash
kubectl get gcppubsubsource
NAME             AGE
testing-source   3d

kubectl get channel
NAME          AGE
pubsub-test   3d
```

## Hello World - .NET Core sample

Let's now create a .NET Core version of that sample. Create an empty ASP.NET Core app:

```bash
dotnet new web -o message-dumper-csharp
```
Inside the `message-dumper-csharp` folder, update [Startup.cs](../eventing/message-dumper-csharp/Startup.up) to have a logger to print the contents of the event:

```csharp
using System.IO;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace message_dumper_csharp
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
                    var content = reader.ReadToEnd();
                    _logger.LogInformation("C# Message Dumper received message: " + content);
                    await context.Response.WriteAsync(content);
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

## Build and push Docker image

Before building the Docker image, make sure the app has no compilation errors:

```bash
dotnet build
```

Create a [Dockerfile](../eventing/message-dumper-csharp/Dockerfile) for the image:

```
FROM microsoft/dotnet:2.2-sdk

WORKDIR /app
COPY *.csproj .
RUN dotnet restore

COPY . .

RUN dotnet publish -c Release -o out

ENV PORT 8080

ENV ASPNETCORE_URLS http://*:${PORT}

CMD ["dotnet", "out/message-dumper-csharp.dll"]
```

Build and push the Docker image (replace `meteatamel` with your actual DockerHub): 

```docker
docker build -t meteatamel/message-dumper-csharp:v1 .

docker push meteatamel/message-dumper-csharp:v1
```

## Deploy the service and subscription

Create a [subscriber.yaml](../eventing/message-dumper-csharp/subscriber.yaml) file.

```yaml
apiVersion: serving.knative.dev/v1alpha1
kind: Service
metadata:
  name: message-dumper-csharp
spec:
  runLatest:
    configuration:
      revisionTemplate:
        spec:
          container:
            # Replace meteatamel with your actual DockerHub
            image: docker.io/meteatamel/message-dumper-csharp:v1

---
apiVersion: eventing.knative.dev/v1alpha1
kind: Subscription
metadata:
  name: gcppubsub-source-sample-csharp
spec:
  channel:
    apiVersion: eventing.knative.dev/v1alpha1
    kind: Channel
    name: pubsub-test
  subscriber:
    ref:
      apiVersion: serving.knative.dev/v1alpha1
      kind: Service
      name: message-dumper-csharp
```
This defines the Knative Service that will run our code and Subscription to connect to Pub/Sub messages via the Channel.

```bash
kubectl apply -f subscriber.yaml
```

Check that the service is created:

```bash
kubectl get ksvc message-dumper-csharp
NAME            AGE
message-dumper-csharp   21s  
```
## Test the service

We can now test our service by sending a message to Pub/Sub topic:

```bash
gcloud pubsub topics publish testing --message="Hello World"
```

Wait a little and check that a pod is created:

```bash
kubectl get pods --selector serving.knative.dev/service=message-dumper-csharp
```
You can inspect the logs of the subscriber (replace `<podid>` with actual pod id):

```bash
kubectl logs --follow -c user-container message-dumper-00001-deployment-<podid>
```
You should see something similar to this:

```bash
Hosting environment: Production
Content root path: /app
Now listening on: http://0.0.0.0:8080
Application started. Press Ctrl+C to shut down.
Application is shutting down...
Hosting environment: Production
Content root path: /app
Now listening on: http://0.0.0.0:8080
Application started. Press Ctrl+C to shut down.
info: Microsoft.AspNetCore.Hosting.Internal.WebHost[1]
      Request starting HTTP/1.1 POST http://message-dumper-csharp.default.svc.cluster.local/ application/json 108
info: message_dumper_csharp.Startup[0]
      C# Message Dumper received message: {"ID":"198012587785403","Data":"SGVsbG8gV29ybGQ=","Attributes":null,"PublishTime":"2019-01-21T15:25:58.25Z"}
info: Microsoft.AspNetCore.Hosting.Internal.WebHost[2]
      Request finished in 29.9881ms 200 
```
Finally, if you decode the `Data` field, you should see the "Hello World" message:

```bash
echo "SGVsbG8gV29ybGQ=" | base64 --decode
Hello World
```

## What's Next?
[Integrate with Translation API](07-translationeventing.md)