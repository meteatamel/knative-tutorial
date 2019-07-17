# Create Twilio SMS handler - C#

## Create SmsController

Start with creating an empty ASP.NET Core app:

```bash
dotnet new web -o twiliosample
```

Inside the `twilio` folder, change [Startup.cs](../serving/twilio/csharp/Startup.cs) to use MVC:

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

Finally, we can create [SmsController.cs](../serving/twilio/csharp/SmsController.cs) to receive SMS messages from Twilio:

```csharp
using Microsoft.AspNetCore.Mvc;
using Twilio.AspNet.Common;
using Twilio.AspNet.Core;
using Twilio.TwiML;

namespace twiliosample
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

Make sure the app has no compilation errors:

```bash
dotnet build
```

## Create a Dockerfile

Create a [Dockerfile](../serving/twilio/csharp/Dockerfile) for the image:

```dockerfile
FROM microsoft/dotnet:2.2-sdk

WORKDIR /app
COPY *.csproj .
RUN dotnet restore

COPY . .

RUN dotnet publish -c Release -o out

ENV PORT 8080

ENV ASPNETCORE_URLS http://*:${PORT}

CMD ["dotnet", "out/twiliosample.dll"]
```

## What's Next?

[Back to Integrate with Twilio](06-twiliointegration.md)
