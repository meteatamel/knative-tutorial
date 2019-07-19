# Integrate with Vision API

[Cloud Vision API](https://cloud.google.com/vision/docs) is another Machine Learning API of Google Cloud. You can use it to derive insight from your images with powerful pre-trained API models or easily train custom vision models with AutoML Vision.

In this lab, we will use a [Cloud Storage](https://cloud.google.com/storage/docs/) bucket to store our images. We will also enable [Pub/Sub notifications](https://cloud.google.com/storage/docs/pubsub-notifications) on our bucket. This way, every time we add an image to the bucket, it will trigger a Pub/Sub message. This in turn will trigger our Knative service where we will use Vision API to analyze the image. Pretty cool!

Since we're making calls to Google Cloud services, you need to make sure that the outbound network access is enabled, as described [here](https://github.com/knative/docs/blob/master/serving/outbound-network-access.md).

You also want to make sure that the Vision API is enabled:

```bash
gcloud services enable vision.googleapis.com
```

## Create a Vision Handler

Follow the instructions for your preferred language to create a service to handle translation messages:

* [Create Vision Handler - C#](10-visioneventing-csharp.md)

## Build and push Docker image

Build and push the Docker image (replace `{username}` with your actual DockerHub):

```bash
docker build -t {username}/vision:v1 .

docker push {username}/vision:v1
```

## Deploy the service and trigger

Create a [trigger.yaml](../eventing/vision/trigger.yaml) file.

```yaml
apiVersion: serving.knative.dev/v1beta1
kind: Service
metadata:
  name: vision
  namespace: default
spec:
  template:
    spec:
      containers:
        # Replace {username} with your actual DockerHub
        - image: docker.io/{username}/vision:v1
---
apiVersion: eventing.knative.dev/v1alpha1
kind: Trigger
metadata:
  name: vision
spec:
  subscriber:
    ref:
      apiVersion: serving.knative.dev/v1beta1
      kind: Service
      name: vision
```

This defines the Knative Service that will run our code and Trigger to connect to Pub/Sub messages.

```bash
kubectl apply -f trigger.yaml
```

Check that the service and trigger are created:

```bash
kubectl get ksvc,trigger
```

## Create bucket and enabled notifications

Before we can test the service, let's first create a Cloud Storage bucket. You can do this [in many ways](https://cloud.google.com/storage/docs/creating-buckets). We'll use `gsutil` as follows (replace `knative-bucket` with a unique name):

```bash
gsutil mb gs://knative-bucket/
```

Once the bucket is created, enable Pub/Sub notifications on it and link to our `testing` topic we created in earlier labs:

```bash
gsutil notification create -t testing -f json gs://knative-bucket/
```

Check that the notification is created:

```bash
gsutil notification list gs://knative-bucket
```

## Test the service

We can finally test our Knative service by uploading an image to the bucket.

First, let's watch the logs of the service. Wait a little and check that a pod is created:

```bash
kubectl get pods --selector serving.knative.dev/service=vision
```

You can inspect the logs of the subscriber:

You can inspect the logs of the subscriber (replace `<podid>` with actual pod id):

```bash
kubectl logs --follow -c user-container <podid>
```

Drop the image to the bucket in Google Cloud Console or use `gsutil` to copy the file as follows:

```bash
gsutil cp pics/beach.jpg gs://knative-bucket/
```

This triggers a Pub/Sub message to our Knative service.

You should see something similar to this in logs:

```bash
info: vision.Startup[0]
      This picture is labelled: Sky,Body of water,Sea,Nature,Coast,Water,Sunset,Horizon,Cloud,Shore
info: Microsoft.AspNetCore.Hosting.Internal.WebHost[2]
      Request finished in 1948.3204ms 200
```

## What's Next?

[Hello World Build](11-helloworldbuild.md)
