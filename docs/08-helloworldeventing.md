# Hello World Eventing

As of v0.5, Knative Eventing defines Broker and Trigger to receive and filter messages. This is explained in more detail on [Knative Eventing](https://www.knative.dev/docs/eventing/) page:

![Broker and Trigger](https://www.knative.dev/docs/eventing/images/broker-trigger-overview.svg)

Knative Eventing has a few different types of event sources (Kubernetes, GitHub, GCP Pub/Sub etc.). In this tutorial, we will focus on GCP Pub/Sub events.

## Install Knative Eventing

You probably installed [Knative Eventing](https://www.knative.dev/docs/eventing/) when you [installed Knative](https://www.knative.dev/docs/install/). If not, follow the Knative installation instructions and take a look at the installation section in [Knative Eventing](https://www.knative.dev/docs/eventing/) page. In the end, you should have pods running in `knative-eventing`. Double check that this is the case:

```bash
kubectl get pods -n knative-eventing
```

## Configuring outbound network access

In Knative, the outbound network access is disabled by default. This means that you cannot even call Google Cloud APIs from Knative.

In our samples, we want to call Google Cloud APIs, so make sure you follow instructions on [Configuring outbound network access](https://www.knative.dev/docs/serving/outbound-network-access/) page to enable access.

## Setup Google Cloud Pub/Sub event source and default Broker

Follow the instructions on [GCP Cloud Pub/Sub source](https://www.knative.dev/docs/eventing/samples/gcp-pubsub-source/) page to setup Google Cloud Pub/Sub event source and also have a Broker injected in the default namespace. But don't create the trigger, we'll do that here.

In the end, you should have a GCP Pub/Sub source setup:

```bash
kubectl get gcppubsubsource

NAME             AGE
testing-source   2m
```

And a default broker as well:

```bash
kubectl get broker

NAME      READY   REASON   HOSTNAME                                   AGE
default   True             default-broker.default.svc.cluster.local   12m
```

## Create a Message Dumper

Follow the instructions for your preferred language to create a service to log out messages:

* [Create Message Dumper - C#](08-helloworldeventing-csharp.md)

* [Create Message Dumper - Python](08-helloworldeventing-python.md)

## Build and push Docker image

Build and push the Docker image (replace `{username}` with your actual DockerHub):

```bash
docker build -t {username}/message-dumper:v1 .

docker push {username}/message-dumper:v1
```

## Deploy the service and create a trigger

Create a [trigger.yaml](../eventing/message-dumper/trigger.yaml) file.

```yaml
apiVersion: serving.knative.dev/v1beta1
kind: Service
metadata:
  name: message-dumper
  namespace: default
spec:
  template:
    spec:
      containers:
        # Replace {username} with your actual DockerHub
        - image: docker.io/{username}/message-dumper:v1
---
apiVersion: eventing.knative.dev/v1alpha1
kind: Trigger
metadata:
  name: message-dumper
spec:
  subscriber:
    ref:
      apiVersion: serving.knative.dev/v1alpha1
      kind: Service
      name: message-dumper
```

This defines the Knative Service that will run our code and Trigger to connect to Pub/Sub messages to the Service.

```bash
kubectl apply -f trigger.yaml
```

Check that the service and trigger are created:

```bash
kubectl get ksvc,trigger
```

## Test the service

We can now test our service by sending a message to Pub/Sub topic:

```bash
gcloud pubsub topics publish testing --message="Hello World"

messageIds:
- '198012587785403'
```

Wait a little and check that a pod is created:

```bash
kubectl get pods --selector serving.knative.dev/service=message-dumper
```

You can inspect the logs of the subscriber (replace `<podid>` with actual pod id):

```bash
kubectl logs --follow -c user-container <podid>
```

You should see something similar to this:

* C#

  ```text
  Hosting environment: Production
  Content root path: /app
  Now listening on: http://0.0.0.0:8080
  Application started. Press Ctrl+C to shut down.
  Application is shutting down...
  Hosting environment: Production
  Content root path: /app
  Now listening on: http://0.0.0.0:8080
  Application started. Press Ctrl+C to shut down.
  info: Microsoft.AspNetCore.Hosting.Internal.WebHost[1]
        Request starting HTTP/1.1 POST http://message-dumper.default.svc.cluster.local/ application/json 108
  info: message_dumper.Startup[0]
        Message Dumper received message: {"ID":"198012587785403","Data":"SGVsbG8gV29ybGQ=","Attributes":null,"PublishTime":"2019-01-21T15:25:58.25Z"}
  info: Microsoft.AspNetCore.Hosting.Internal.WebHost[2]
        Request finished in 29.9881ms 200
  ```

* Python

  ```text
  [INFO] Starting gunicorn 19.9.0
  [INFO] Listening at: http://0.0.0.0:8080 (1)
  [INFO] Using worker: threads
  [INFO] Booting worker with pid: 8
  [INFO] Message Dumper received message:
  {'ID': '198012587785403', 'Data': 'SGVsbG8gV29ybGQ=', 'Attributes': None, 'PublishTime': '2019-01-21T15:25:58.25Z'}
  ```

Finally, if you decode the `Data` field, you should see the "Hello World" message:

```bash
echo "SGVsbG8gV29ybGQ=" | base64 -D
Hello World
```

## What's Next?

[Integrate with Translation API](09-translationeventing.md)
