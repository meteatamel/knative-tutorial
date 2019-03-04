# Integrate with Twilio

You can use [Twilio](https://www.twilio.com/) to embed voice, VoIP, and messaging into applications. In this lab, we will use a [Webhook](https://www.twilio.com/docs/glossary/what-is-a-webhook) to reply to SMS messages sent to a Twilio phone number. The Webhook is going to be a Knative service running on GKE.

## Twilio Setup

You need to [create a Twilio account](https://www.twilio.com/try-twilio) and get a [Twilio phone number](https://www.twilio.com/docs/usage/tutorials/how-to-use-your-free-trial-account#get-your-first-twilio-phone-number). You need to make sure that the Twilio number is SMS enabled. 

In Twilio [console](https://www.twilio.com/console), click on the phone number and go to `Messaging` section. There's a Webhook defined for when a message comes in. We will change that to our Knative Service later:

![Twilio Webhook](./images/twilio-webhook.png)

## Twilio - .NET Core sample 

Let's start with creating an empty ASP.NET Core app:

```bash
dotnet new web -o twilio-csharp
```
Inside the `twilio-csharp` folder, change [Startup.cs](../serving/twilio-csharp/Startup.cs) to use MVC:

```csharp
public void ConfigureServices(IServiceCollection services)
{
    services.AddMvc();
}

public void Configure(IApplicationBuilder app, IHostingEnvironment env)
{
    if (env.IsDevelopment())
    {
        app.UseDeveloperExceptionPage();
    }
    
    app.UseMvc();
}
```

Next, let's install [twilio-aspnet](https://github.com/twilio/twilio-aspnet) package:

```bash
dotnet add package Twilio.AspNet.Core 
```
Finally, we can create [SmsController.cs](../serving/twilio-csharp/SmsController.cs) to receive SMS messages from Twilio:

```csharp
using Microsoft.AspNetCore.Mvc;
using Twilio.AspNet.Common;
using Twilio.AspNet.Core;
using Twilio.TwiML;

namespace twilio_csharp
{
    [Route("[controller]")]
    public class SmsController : TwilioController
    {
        [HttpGet]
        public TwiMLResult Index(SmsRequest incomingMessage)
        {
            var messagingResponse = new MessagingResponse();
            messagingResponse.Message("The Knative copy cat says: " + incomingMessage.Body);
            return TwiML(messagingResponse);
        }
    }
}
```
`SmsController.cs` simply echoes back the received message. 

## Build and push Docker image

Before building the Docker image, make sure the app has no compilation errors:

```bash
dotnet build
```

Create a [Dockerfile](../serving/twilio-csharp/Dockerfile) for the image:

```
FROM microsoft/dotnet:2.2-sdk

WORKDIR /app
COPY *.csproj .
RUN dotnet restore

COPY . .

RUN dotnet publish -c Release -o out

ENV PORT 8080

ENV ASPNETCORE_URLS http://*:${PORT}

CMD ["dotnet", "out/twilio-csharp.dll"]
```

Build and push the Docker image (replace `meteatamel` with your actual DockerHub): 

```docker
docker build -t meteatamel/twilio-csharp:v1 .

docker push meteatamel/twilio-csharp:v1
```

## Deploy the Knative service

Create a [service.yaml](../serving/twilio-csharp/service.yaml) file.

```yaml
apiVersion: serving.knative.dev/v1alpha1
kind: Service
metadata:
  name: twilio-csharp
  namespace: default
spec:
  runLatest:
    configuration:
      revisionTemplate:
        spec:
          container:
            # Replace meteatamel with your actual DockerHub
            image: docker.io/meteatamel/twilio-csharp:v1
```

After the container is pushed, deploy the app. 

```bash
kubectl apply -f service.yaml
```

Check that the service is created:

```bash
kubectl get ksvc twilio-csharp
NAME            AGE
twilio-csharp   21s  
```
## Test the service

We can finally test our service by sending an SMS to our Twilio number. We need to setup Twilio Webhook first.

In Twilio [console](https://www.twilio.com/console), click on the phone number and go to `Messaging` section. For Webhook defined for when a message comes in, change it to your Knative service name and NIP.IO domain:

![Twilio Webhook](./images/twilio-webhook-custom.png)

Now, you can send an SMS message to your Twilio number and you should get a reply back from the Knative service!

## What's Next?
[Hello World Eventing](06-helloworldeventing.md)