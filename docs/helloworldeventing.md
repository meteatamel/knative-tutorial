# Hello World Eventing

Knative Eventing defines Broker and Trigger to receive and filter messages. This is explained in more detail on [Knative Eventing](https://www.knative.dev/docs/eventing/) page:

![Broker and Trigger](https://www.knative.dev/docs/eventing/images/broker-trigger-overview.svg)

Knative Eventing has a few different types of [event sources](https://knative.dev/docs/eventing/sources/) (Kubernetes, GitHub, GCP Pub/Sub etc.) that it can listen. 

In this tutorial, we will create our own events using an event producer and listen and log the messages in an event consumer. This tutorial is based on [Getting Started with Knative Eventing](https://knative.dev/docs/eventing/getting-started/) with slight modifications to make it easier.

## Knative Eventing

First, make sure Knative Eventing is installed:

```bash
kubectl get pods -ns knative-eventing

NAME                                   READY   STATUS    RESTARTS   AGE
eventing-controller-7d75cd8598-brsnv   1/1     Running   0          14d
eventing-webhook-5cb89d8974-4csl9      1/1     Running   0          14d
imc-controller-654d689bc9-zfxgf        1/1     Running   0          14d
imc-dispatcher-794f546c85-5pqgt        1/1     Running   0          14d
sources-controller-67788d5b86-c5bjf    1/1     Running   0          14d
```

If not, you can follow the instructions on Knative Eventing Installation [page](https://knative.dev/docs/eventing/getting-started/#installing-knative-eventing). 

## Broker

We need to inject a Broker in the namespace where we want to receive messages. Let's create a separate namespace and label it to get Broker injected:

```bash
kubectl create namespace event-example

kubectl label namespace event-example knative-eventing-injection=enabled
```

You should see a Broker in the namespace:

```bash
kubectl get broker -n event-example

NAME      READY   REASON   URL                                                     AGE
default   True             http://default-broker.event-example.svc.cluster.local   55s
```

## Consumer

### Event Display

For event consumer, we'll use an Event Display service that simply logs out received messages. Follow the instructions for your preferred language to create a service to log out messages:

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
kubectl -n event-example apply -f service.yaml

deployment.apps/event-display created
service/event-display created
```

## Trigger

Let's connect the Event Display service to the Broker with a Trigger. 

Create a [trigger-event-display.yaml](../eventing/event-display/trigger-event-display.yaml):

```yaml
apiVersion: eventing.knative.dev/v1alpha1
kind: Trigger
metadata:
  name: trigger-event-display
spec:
  filter:
    attributes:
      type: event-display
  subscriber:
    ref:
      apiVersion: v1
      kind: Service
      name: event-display
```

Notice that we're filtering with the required attribute `type` with value `event-display`. Only messages with this attribute will be sent to the `event-display` service. 

Create the trigger:

```bash
kubectl -n event-example apply -f trigger-event-display.yaml

trigger.eventing.knative.dev/trigger-event-display created
```

Check that the trigger is ready:

```bash
kubectl -n event-example get trigger

NAME                    READY   REASON   BROKER    SUBSCRIBER_URI                                          AGE
trigger-event-display   True             default   http://event-display.event-example.svc.cluster.local/   23s
```

## Producer

You can only access the Broker from within your Eventing cluster. Normally, you would create a Pod within that cluster to act as your event producer. In this case, we'll simply create a Pod with curl installed and use curl to manually send messages. 

### Curl Pod

Create a [curl-pod.yaml](../eventing/event-display/curl-pod.yaml) file:

```yaml
apiVersion: v1
kind: Pod
metadata:
  labels:
    run: curl
  name: curl
spec:
  containers:
  - image: radial/busyboxplus:curl
    imagePullPolicy: IfNotPresent
    name: curl
    resources: {}
    stdin: true
    terminationMessagePath: /dev/termination-log
    terminationMessagePolicy: File
    tty: true
```

Create the pod:

```bash
kubectl -n event-example apply -f curl-pod.yaml

pod/curl created
```

### Send events to Broker

SSH into the pod:

```bash
kubectl -n event-example attach curl -it
Defaulting container name to curl.
Use 'kubectl describe pod/ -n event-example' to see all of the containers in this pod.
If you don't see a command prompt, try pressing enter.
[ root@curl:/ ]$
```

Send the event. Notice that we're sending with event type `event-display`:

```bash
curl -v "http://default-broker.event-example.svc.cluster.local" \
  -X POST \
  -H "Ce-Id: say-hello" \
  -H "Ce-Specversion: 0.3" \
  -H "Ce-Type: event-display" \
  -H "Ce-Source: curl-pod" \
  -H "Content-Type: application/json" \
  -d '{"msg":"Hello Knative1!"}'
```

You should get HTTP 202 back:

```bash
< HTTP/1.1 202 Accepted
< Content-Length: 0
< Date: Fri, 29 Nov 2019 13:06:17 GMT
```

The logs of the Event Display pod should show the message:

```bash
kubectl -n event-example logs event-display-84485c6d9d-ttfp9

info: event_display.Startup[0]
      Event Display received event: {"msg":"Hello Knative1!"}
```

If you send another message without `event-display` type, that won't trigger the Event Display. 

## What's Next?

[Hello World Eventing with GCP Pub/Sub](helloworldeventingpubsub.md)
