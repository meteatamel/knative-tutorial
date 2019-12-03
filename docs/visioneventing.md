# Integrate with Vision API

[Cloud Vision API](https://cloud.google.com/vision/docs) is another Machine Learning API of Google Cloud. You can use it to derive insight from your images with powerful pre-trained API models or easily train custom vision models with AutoML Vision.

In this lab, we will use a [Cloud Storage](https://cloud.google.com/storage/docs/) bucket to store our images. We will also enable [Pub/Sub notifications](https://cloud.google.com/storage/docs/pubsub-notifications) on our bucket. This way, every time we add an image to the bucket, it will trigger a Pub/Sub message. This in turn will trigger our service where we will use Vision API to analyze the image.

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

Create a [service.yaml](../eventing/vision/service.yaml) file.

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: vision
spec:
  selector:
    matchLabels:
      app: vision
  template:
    metadata:
      labels:
        app: vision
    spec:
      containers:
      - name: user-container
        # Replace {username} with your actual DockerHub
        image: docker.io/{username}/vision:v1
        imagePullPolicy: Always
        ports:
        - containerPort: 8080
---
apiVersion: v1
kind: Service
metadata:
  name: vision
spec:
  selector:
    app: vision
  ports:
  - protocol: TCP
    port: 80
    targetPort: 8080
```

This defines a Kubernetes Service to receive messages. 

Create the Vision service:

```bash
kubectl apply -f service.yaml

deployment.apps/vision created
service/vision created
```

## Create a trigger

We need connect Vision service to the Broker/ 

Create a [trigger.yaml](../eventing/vision/trigger.yaml):

```yaml
apiVersion: eventing.knative.dev/v1alpha1
kind: Trigger
metadata:
  name: trigger-vision
spec:
  subscriber:
    ref:
      #apiVersion: serving.knative.dev/v1
      apiVersion: v1
      kind: Service
      name: vision
```

Create the trigger:

```bash
kubectl apply -f trigger.yaml

trigger.eventing.knative.dev/trigger-vision created
```

## Create bucket and enable notifications

Before we can test the service, let's first create a Cloud Storage bucket. You can do this [in many ways](https://cloud.google.com/storage/docs/creating-buckets). We'll use `gsutil` as follows:

```bash
# Unique bucket name
export VISION_BUCKET="$(gcloud config get-value core/project)-vision"

gsutil mb gs://$VISION_BUCKET

Creating gs://VISION_BUCKET/...
```

Once the bucket is created, enable Pub/Sub notifications on it for object updates and link to our `testing` topic we created in earlier labs:

```bash
gsutil notification create -t testing -f json -e OBJECT_FINALIZE gs://knative-bucket

Created notification config projects/_/buckets/VISION_BUCKET/notificationConfigs/1
```

Check that the notification is created:

```bash
gsutil notification list gs://$VISION_BUCKET

projects/_/buckets/VISION_BUCKET/notificationConfigs/1
        Cloud Pub/Sub topic: projects/PROJECT_ID/topics/testing
```

## Test the service

We can finally test our service by uploading an image to the bucket.

First, let's watch the logs of the service. Wait a little and check that a pod is created:

```bash
kubectl get pods
```

You can inspect the logs of the subscriber (replace `<podid>` with actual pod id):

```bash
kubectl logs --follow <podid>
```

Drop the image to the bucket in Google Cloud Console or use `gsutil` to copy the file as follows:

```bash
gsutil cp pics/beach.jpg gs://$VISION_BUCKET
```

This triggers a Pub/Sub message to our service.

You should see something similar to this:

```text
This picture is labelled: Sky,Body of water,Sea,Nature,Coast,Water,Sunset,Horizon,Cloud,Shore
```
