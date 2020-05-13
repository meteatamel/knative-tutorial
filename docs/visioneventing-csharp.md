# Create Vision Handler - C#

## Create Vision Handler

Let's start with creating an empty ASP.NET Core app:

```bash
dotnet new web -o vision
```

Inside the `vision/csharp` folder, update [Startup.cs](../eventing/vision/csharp/Startup.cs) to match with what we have.

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

## Add Vision API

Add Vision API NuGet package to our project:

```bash
dotnet add package Google.Cloud.Vision.V1
```

[Startup.cs](../eventing/vision/csharp/Startup.cs) checks for `storage#object` events. These events are emitted by Cloud Storage when a file is uploaded.

```csharp
dynamic json = JValue.Parse(content);
if (json == null) return;

var kind = json.kind;
if (kind == null || kind != "storage#object") return;
```

Next, we extract the Cloud Storage URL of the file from the event:

```csharp
var storageUrl = (string)ConstructStorageUrl(json);

private string ConstructStorageUrl(dynamic json)
{
    return json == null? null 
        : string.Format("gs://{0}/{1}", json.bucket, json.name);
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

Before building the Docker image, make sure the app has no compilation errors:

```bash
dotnet build
```

## Create Dockerfile

Create a [Dockerfile](../eventing/vision/csharp/Dockerfile) for the image.

## What's Next?

Back to [Integrate with Vision API](visioneventing.md)
