# Create Translation Handler - C#

## Create Translation Handler

Let's start with creating an empty ASP.NET Core app:

```bash
dotnet new web -o translation
```

Inside the `translation/csharp` folder, update [Startup.cs](../eventing/translation/csharp/Startup.cs) to match with what we have.

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

## Handle translation protocol

To encapsulate translation protocol, create a [TranslationRequest.cs](../eventing/translation/csharp/TranslationRequest.cs) class.

## Add Translation API

Add Translation API NuGet package to our project:

```bash
dotnet add package Google.Cloud.Translation.V2
```

[Startup.cs](../eventing/translation/csharp/Startup.cs) extracts `TranslationRequest` out of the request.  

```csharp
var translationRequest = JsonConvert.DeserializeObject<TranslationRequest>(decodedData);
```

Once we have the translation request, we pass that to Translation API:

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

You can see the full code in [Startup.cs](../eventing/translation/csharp/Startup.cs).

Before building the Docker image, make sure the app has no compilation errors:

```bash
dotnet build
```

## Create Dockerfile

Create a [Dockerfile](../eventing/translation/csharp/Dockerfile) for the image.

## What's Next?

Back to [Integrate with Translation API](translationeventing.md)