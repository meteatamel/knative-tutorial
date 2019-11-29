# Create Twilio SMS handler - C#

## Create SmsController

Start with creating an empty ASP.NET Core app:

```bash
dotnet new web -o twiliosample
```

Inside the `twilio` folder, change [Startup.cs](../serving/twilio/csharp/Startup.cs) to use controllers:

```csharp
public void ConfigureServices(IServiceCollection services)
{
    services.AddControllers();
}

public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
{
    if (env.IsDevelopment())
    {
        app.UseDeveloperExceptionPage();
    }

    app.UseRouting();

    app.UseEndpoints(endpoints =>
    {
        endpoints.MapDefaultControllerRoute();
    });
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
# Use Microsoft's official build .NET image.
# https://hub.docker.com/_/microsoft-dotnet-core-sdk/
FROM mcr.microsoft.com/dotnet/core/sdk:3.0-alpine AS build
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
FROM mcr.microsoft.com/dotnet/core/aspnet:3.0-alpine AS runtime
WORKDIR /app
COPY --from=build /app/out ./

# Run the web service on container startup.
ENTRYPOINT ["dotnet", "twiliosample.dll"]
```

## What's Next?

[Back to Integrate with Twilio](twiliointegration.md)
