# Hello World Eventing

As of v0.5, Knative Eventing defines Broker and Trigger to receive and filter messages. This is explained in more detail on [Knative Eventing](https://www.knative.dev/docs/eventing/) page:

![Broker and Trigger](https://www.knative.dev/docs/eventing/images/broker-trigger-overview.svg)

Under the covers, Knative creates Channels and Subscriptions and supports direct and fanout delivery options:

![Diagram](https://raw.githubusercontent.com/knative/docs/master/docs/eventing/images/control-plane.png)

Knative Eventing has a few different types of event sources (Kubernetes, GitHub, GCP Pub/Sub etc.). In this tutorial, we will focus on GCP Pub/Sub events. 

## Install Knative Eventing

You probably installed [Knative Eventing](https://www.knative.dev/docs/eventing/) when you [installed Knative](https://www.knative.dev/docs/install/). If not, follow the Knative installation instructions and take a look at the installation section in [Knative Eventing](https://www.knative.dev/docs/eventing/) page. In the end, you should have pods running in `knative-eventing` and `knative-sources` namespaces. Double check that this is the case:

```bash
kubectl get pods -n knative-eventing
kubectl get pods -n knative-sources
```

## Configuring outbound network access

In Knative, the outbound network access is disabled by default. This means that you cannot even call Google Cloud APIs from Knative. 

In our samples, we want to call Google Cloud APIs, so make sure you follow instructions on [Configuring outbound network access](https://www.knative.dev/docs/serving/outbound-network-access/) page to enable access. 

## Setup Google Cloud Pub/Sub event source and default Broker

Follow the instructions on [GCP Cloud Pub/Sub source](https://www.knative.dev/docs/eventing/samples/gcp-pubsub-source/) page to setup Google Cloud Pub/Sub event source and also have a Broker injected in the default namespace. But don't create the trigger, we'll do that here. 

In the end, you should have a GCP Pub/Sub source setup:

```bash
kubectl get gcppubsubsource

NAME             AGE
testing-source   1d
```

And a default broker as well:

```bash
kubectl get broker

NAME      READY   REASON   HOSTNAME                                   
default   True             default-broker.default.svc.cluster.local   
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

## Deploy the service and create a trigger

Create a [trigger.yaml](../eventing/message-dumper-csharp/trigger.yaml) file.

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
        metadata:
          annotations:
            # Disable scale to zero with a minScale of 1.
            autoscaling.knative.dev/minScale: "1"
---
apiVersion: eventing.knative.dev/v1alpha1
kind: Trigger
metadata:
  name: gcppubsub-source-sample-csharp
spec:
  subscriber:
    ref:
      apiVersion: serving.knative.dev/v1alpha1
      kind: Service
      name: message-dumper-csharp
```

This defines the Knative Service that will run our code and Trigger to connect to Pub/Sub messages to the Service.

```bash
kubectl apply -f trigger.yaml
```

Check that the service and trigger are created:

```bash
kubectl get ksvc,trigger
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
kubectl logs --follow -c user-container <podid>
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
echo "SGVsbG8gV29ybGQ=" | base64 -D
Hello World
```

## What's Next?
[Integrate with Translation API](07-translationeventing.md)