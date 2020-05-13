# Create Event Display - C#

## Create Event Display

Create an empty ASP.NET Core app:

```bash
dotnet new web -o event-display
```

Inside the `event-display/csharp` folder, update [Startup.cs](../eventing/event-display/csharp/Startup.cs) to have a logger to print the contents of the event. 

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

Create a [Dockerfile](../eventing/event-display/csharp/Dockerfile) for the image.

## What's Next?

Back to [Hello World Eventing](helloworldeventing.md)
