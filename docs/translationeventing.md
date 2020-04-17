# Integrate with Translation API

In the [previous lab](helloworldeventing.md), our service simply logged out the received Pub/Sub event. While this might be useful for debugging, it's not terribly exciting.

[Cloud Translation API](https://cloud.google.com/translate/docs/) is one of Machine Learning APIs of Google Cloud. It can dynamically translate text between thousands of language pairs. In this lab, we will use translation requests sent via Pub/Sub messages and use Translation API to translate text between languages.

## Pub/Sub triggered service

We're assuming that you already went through [Pub/Sub triggered
service](pubsubeventing.md) tutorial and already setup Knative with GCP & PubSub
Topic and also have a `CloudPubSubSource` defined.

## Enable Translation API

You also want to make sure that the Translation API is enabled:

```bash
gcloud services enable translate.googleapis.com
```

## Define translation protocol

Let's first define the translation protocol we'll use in our sample. The body of Pub/Sub messages will include text and the languages to translate from and to as follows:

```text
{"text":"Hello World", "from":"en", "to":"es"} => English to Spanish
{"text":"Hello World", "from":"", "to":"es"} => Detected language to Spanish
{"text":"Hello World", "from":"", "to":""} => Error
```

## Create a Translation Handler

Follow the instructions for your preferred language to create a service to handle translation messages:

* [Create Translation Handler - C#](translationeventing-csharp.md)

* [Create Translation Handler - Python](translationeventing-python.md)

## Build and push Docker image

Build and push the Docker image (replace `{username}` with your actual DockerHub):

```bash
docker build -t {username}/translation:v1 .

docker push {username}/translation:v1
```

## Deploy the Translation service

Create a [kservice.yaml](../eventing/translation/kservice.yaml).

This defines a Knative Service to receive messages.

```bash
kubectl apply -f kservice.yaml
```

## Create a trigger

Last but not least, we need connect Translation service to Broker with a trigger.

Create a [trigger.yaml](../eventing/translation/trigger.yaml):

This connects the `testing` topic to `translation` service.

Create the trigger:

```bash
kubectl apply -f trigger.yaml
```

## Test the service

We can now test our service by sending a translation request message to Pub/Sub topic:

```bash
gcloud pubsub topics publish testing --message='{"text":"Hello World", "from":"en", "to":"es"}'
```

Wait a little and check that a pod is created:

```bash
kubectl get pods
```

You can inspect the logs of the subscriber (replace `<podid>` with actual pod id):

```bash
kubectl logs --follow <podid>
```

You should see something similar to this:

```text
Received content: {"text":"Hello World", "from":"en", "to":"es"}

Translated text: Hola Mundo
```