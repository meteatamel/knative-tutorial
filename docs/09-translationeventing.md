# Integrate with Translation API

In the [previous lab](08-helloworldeventing.md), our Knative service simply logged out the received Pub/Sub event. While this might be useful for debugging, it's not terribly exciting. 

[Cloud Translation API](https://cloud.google.com/translate/docs/) is one of Machine Learning APIs of Google Cloud. It can dynamically translate text between thousands of language pairs. In this lab, we will use translation requests sent via Pub/Sub messages and use Translation API to translate text between languages. 

Since we're making calls to Google Cloud services, you need to make sure that the outbound network access is enabled, as described in the previous lab. 

## Define translation protocol

Let's first define the translation protocol we'll use in our sample. The body of Pub/Sub messages will include text and the languages to translate from and to as follows:

```
{text:'Hello World', from:'en', to:'es'} => English to Spanish
{text:'Hello World', from:'', to:'es'} => Detected language to Spanish
{text:'Hello World', from:'', to:''} => Error
```

## Create a Translation Handler

Follow the instructions for your preferred language to create a service to handle translation messages:

* [Create Translation Handler - C#](09-translationeventing-csharp.md)

## Build and push Docker image

Build and push the Docker image (replace `{username}` with your actual DockerHub): 

```docker
docker build -t {username}/translation:v1 .

docker push {username}/translation:v1
```
## Deploy the service and trigger

Create a [trigger.yaml](../eventing/translation/trigger.yaml) file.

```yaml
apiVersion: serving.knative.dev/v1beta1
kind: Service
metadata:
  name: translation
  namespace: default
spec:
  template:
    spec:
      containers:
        # Replace {username} with your actual DockerHub
        - image: docker.io/{username}/translation:v1
---
apiVersion: eventing.knative.dev/v1alpha1
kind: Trigger
metadata:
  name: translation
spec:
  subscriber:
    ref:
      apiVersion: serving.knative.dev/v1alpha1
      kind: Service
      name: translation
```
This defines the Knative Service that will run our code and Trigger to connect to Pub/Sub messages.

```bash
kubectl apply -f trigger.yaml
```

Check that the service and trigger are created:

```bash
kubectl get ksvc,trigger
```

## Test the service

We can now test our service by sending a translation request message to Pub/Sub topic:

```bash
gcloud pubsub topics publish testing --message="{text:'Hello World', from:'en', to:'es'}"
```

Wait a little and check that a pod is created:

```bash
kubectl get pods --selector serving.knative.dev/service=translation
```
You can inspect the logs of the subscriber (replace `<podid>` with actual pod id):

```bash
kubectl logs --follow -c user-container <podid>
```

You should see something similar to this:

```bash
info: translation.Startup[0]
      Decoded data: {text:'Hello World', from: 'en', to:'es'}
info: translation.Startup[0]
      Calling Translation API
info: translation.Startup[0]
      Translated text: Hola Mundo
info: Microsoft.AspNetCore.Hosting.Internal.WebHost[2]
      Request finished in 767.2586ms 200 
```

## What's Next?
[Integrate with Vision API](10-visioneventing.md)
