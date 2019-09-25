# Create Event Display - C#

## Create Event Display

Create an empty ASP.NET Core app:

```bash
dotnet new web -o event-display
```

Inside the `event-display/csharp` folder, update [Startup.cs](../eventing/event-display/csharp/Startup.cs) to have a logger to print the contents of the event:

```csharp
using System.IO;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace event_display
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
                    _logger.LogInformation("Event Display received message: " + content);
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

Before building the Docker image, make sure the app has no compilation errors:

```bash
dotnet build
```

## Create a Dockerfile

Create a [Dockerfile](../eventing/event-display/csharp/Dockerfile) for the image:

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
ENTRYPOINT ["dotnet", "event-display.dll"]
```

## What's Next?

Back to [Hello World Eventing](08-helloworldeventing.md)
