# Hello World Eventing with GCP Pub/Sub

## GCP PubSub event source

Follow the [GCP Cloud Pub/Sub source](https://knative.dev/docs/eventing/samples/gcp-pubsub-source/) docs page to set Knative Eventing with GCP Pub/Sub up until where you need to create an event display. We'll create our own event display and trigger to connect to it.  

## Consumer

### Event Display

Follow the instructions for your preferred language to create a service to log out messages:

* [Create Event Display - C#](helloworldeventing-csharp.md)

* [Create Event Display - Python](helloworldeventing-python.md)

### Docker image

Build and push the Docker image (replace `{username}` with your actual DockerHub):

```bash
docker build -t {username}/event-display:v1 .

docker push {username}/event-display:v1
```

### Kubernetes Service 

You can have any kind of addressable as event sinks (Kubernetes Service, Knative Service etc.). For this part, let's use a Kubernetes Service.

Create a [service.yaml](../eventing/event-display/service.yaml) file:

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: event-display
spec:
  selector:
    matchLabels:
      app: event-display
  template:
    metadata:
      labels:
        app: event-display
    spec:
      containers:
      - name: user-container
        # Replace {username} with your actual DockerHub
        image: docker.io/{username}/event-display:v1
        ports:
        - containerPort: 8080
---
apiVersion: v1
kind: Service
metadata:
  name: event-display
spec:
  selector:
    app: event-display
  ports:
  - protocol: TCP
    port: 80
    targetPort: 8080
```

This defines a Kubernetes Deployment and Service to receive messages. 

Create the Event Display service:

```bash
kubectl apply -f service.yaml

deployment.apps/event-display created
service/event-display created
```

## Trigger

Let's connect the Event Display service to the Broker with a Trigger. 

Create a [trigger-event-display-pubsub.yaml](../eventing/event-display/trigger-event-display-pubsub.yaml):

```yaml
apiVersion: eventing.knative.dev/v1alpha1
kind: Trigger
metadata:
  name: trigger-event-display-pubsub
spec:
  subscriber:
    ref:
      #apiVersion: serving.knative.dev/v1
      apiVersion: v1
      kind: Service
      name: event-display
```

Create the trigger:

```bash
kubectl apply -f trigger-event-display-pubsub.yaml

trigger.eventing.knative.dev/trigger-event-display-pubsub created
```

Check that the trigger is ready:

```bash
kubectl get trigger

NAME                           READY   REASON   BROKER    SUBSCRIBER_URI                                   AGE
trigger-event-display-pubsub   True             default   http://event-display.default.svc.cluster.local   95s
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
kubectl get pods
```

You can inspect the logs of the pod (replace `<podid>` with actual pod id):

```bash
kubectl logs --follow <podid>
```

You should see something similar to this:

```text
Event Display received message: Hello World
```

## What's Next?

[Integrate with Translation API](translationeventing.md)
