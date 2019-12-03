# Integrate with Translation API

In the [previous lab](helloworldeventing.md), our service simply logged out the received Pub/Sub event. While this might be useful for debugging, it's not terribly exciting.

[Cloud Translation API](https://cloud.google.com/translate/docs/) is one of Machine Learning APIs of Google Cloud. It can dynamically translate text between thousands of language pairs. In this lab, we will use translation requests sent via Pub/Sub messages and use Translation API to translate text between languages.

Since we're making calls to Google Cloud services, you need to make sure that the outbound network access is enabled, as described in the previous lab.

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

Create a [service.yaml](../eventing/translation/service.yaml):

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: translation
spec:
  selector:
    matchLabels:
      app: translation
  template:
    metadata:
      labels:
        app: translation
    spec:
      containers:
      - name: user-container
        # Replace {username} with your actual DockerHub
        image: docker.io/{username}/translation:v1
        imagePullPolicy: Always
        ports:
        - containerPort: 8080
---
apiVersion: v1
kind: Service
metadata:
  name: translation
spec:
  selector:
    app: translation
  ports:
  - protocol: TCP
    port: 80
    targetPort: 8080
```

This defines a Kubernetes Service to receive messages. 

```bash
kubectl apply -f service.yaml

deployment.apps/translation created
service/translation created
```

## Create a trigger

Last but not least, we need connect Translation service to Broker with a trigger. 

Create a [trigger.yaml](../eventing/translation/trigger.yaml):

```yaml
apiVersion: eventing.knative.dev/v1alpha1
kind: Trigger
metadata:
  name: trigger-translation
spec:
  subscriber:
    ref:
      #apiVersion: serving.knative.dev/v1
      apiVersion: v1
      kind: Service
      name: translation
```
This connects the `testing` topic to `translation` service. 

Create the trigger:

```bash
kubectl apply -f trigger.yaml

trigger.eventing.knative.dev/trigger-translation created
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
