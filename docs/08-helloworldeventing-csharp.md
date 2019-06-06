# Create Message Dumper - C#

## Create Message Dumper

Create an empty ASP.NET Core app:

```bash
dotnet new web -o message-dumper
```
Inside the `message-dumper/csharp` folder, update [Startup.cs](../eventing/message-dumper/csharp/Startup.cs) to have a logger to print the contents of the event:

```csharp
using System.IO;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace message_dumper
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
                    _logger.LogInformation("Message Dumper received message: " + content);
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

Create a [Dockerfile](../eventing/message-dumper/csharp/Dockerfile) for the image:

```
FROM microsoft/dotnet:2.2-sdk

WORKDIR /app
COPY *.csproj .
RUN dotnet restore

COPY . .

RUN dotnet publish -c Release -o out

ENV PORT 8080

ENV ASPNETCORE_URLS http://*:${PORT}

CMD ["dotnet", "out/message-dumper.dll"]
```

## What's Next?
Back to [Hello World Eventing](08-helloworldeventing.md)