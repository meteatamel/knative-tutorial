# Integrate with Vision API

[Cloud Vision API](https://cloud.google.com/vision/docs) is another Machine Learning API of Google Cloud. You can use it to derive insight from your images with powerful pre-trained API models or easily train custom vision models with AutoML Vision.

In this lab, we will use a [Cloud
Storage](https://cloud.google.com/storage/docs/) bucket to store our images.
Every time we add an image to the bucket, it will trigger an event to our service where
we will use Vision API to analyze the image.

## Cloud Storage triggered service

We're assuming that you already went through [Cloud Storage triggered
service](storageeventing.md) tutorial and have a bucket and a
`CloudStorageSource` already ready.

## Enable Vision API

You want to make sure that the Vision API is enabled:

```bash
gcloud services enable vision.googleapis.com
```

## Create a Vision Handler

Follow the instructions for your preferred language to create a service to handle cloud storage notifications:

* [Create Vision Handler - C#](visioneventing-csharp.md)

* [Create Vision Handler - Python](visioneventing-python.md)

## Build and push Docker image

Build and push the Docker image (replace `{username}` with your actual DockerHub):

```bash
docker build -t {username}/vision:v1 .

docker push {username}/vision:v1
```

## Create Vision Service

Create a Knative Vision Service defined in
[kservice.yaml](../eventing/vision/kservice.yaml):

```bash
kubectl apply -f kservice.yaml
```

## Create a trigger

We need connect Vision service to the Broker.

Create a [trigger.yaml](../eventing/vision/trigger.yaml).

Create the trigger:

```bash
kubectl apply -f trigger.yaml
```

## Test the service

We can finally test our service by uploading an image to the bucket.

Drop the image to the bucket in Google Cloud Console or use `gsutil` to copy the file as follows:

```bash
gsutil cp ../pictures/beach.jpg gs://${BUCKET}
```

Wait a little and check that a pod is created:

```bash
kubectl get pods
```

Inspect the logs of the subscriber (replace `<podid>` with actual pod id):

```bash
kubectl logs <podid> -c user-container --follow
```

You should see something similar to this:

```text
info: vision.Startup[0]
      Received content: {
        "kind": "storage#object",
        "id": "knative-atamel-storage/beach.jpg/1589382953998973",
        "selfLink": "https://www.googleapis.com/storage/v1/b/knative-atamel-storage/o/beach.jpg",
        "name": "beach.jpg",
        "bucket": "knative-atamel-storage",
        "generation": "1589382953998973",
        "metageneration": "1",
        "contentType": "image/jpeg",
        "timeCreated": "2020-05-13T15:15:53.998Z",
        "updated": "2020-05-13T15:15:53.998Z",
        "storageClass": "STANDARD",
        "timeStorageClassUpdated": "2020-05-13T15:15:53.998Z",
        "size": "2318021",
        "md5Hash": "zxMYWYRr3+/KjFZNxbI5dQ==",
        "mediaLink": "https://www.googleapis.com/download/storage/v1/b/knative-atamel-storage/o/beach.jpg?generation=1589382953998973&alt=media",
        "contentLanguage": "en",
        "crc32c": "OBRvYA==",
        "etag": "CP20ivOQsekCEAE="
      }
info: vision.Startup[0]
      Storage url: gs://knative-atamel-storage/beach.jpg
info: vision.Startup[0]
      This picture is labelled: Sky,Body of water,Sea,Nature,Coast,Water,Sunset
```
