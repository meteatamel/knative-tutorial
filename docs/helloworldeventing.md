# Hello World Eventing

In Knative Eventing, you'd typically use Broker and Trigger to receive and filter messages. This is explained in more detail on [Knative Eventing](https://www.knative.dev/docs/eventing/) page:

![Broker and Trigger](https://www.knative.dev/docs/eventing/images/broker-trigger-overview.svg)

Knative Eventing has a few different types of [event sources](https://knative.dev/docs/eventing/sources/) (Kubernetes, GitHub, GCP Pub/Sub etc.) that it can listen.

In this tutorial, we will create our own events using an event producer and listen and log the messages in an event consumer. This tutorial is based on [Getting Started with Knative Eventing](https://knative.dev/docs/eventing/getting-started/) with slight modifications to make it easier.

## Knative Eventing

First, make sure Knative Eventing is installed:

```bash
kubectl get pods -n knative-eventing

NAME                                   READY   STATUS    RESTARTS   AGE
broker-controller-b85986f7d-xqj2k      1/1     Running   0          4m30s
eventing-controller-58b889c4b4-dnf62   1/1     Running   2          8m55s
eventing-webhook-5549c4b664-jc6x6      1/1     Running   2          8m53s
imc-controller-64cfbf485d-7h2k7        1/1     Running   0          4m32s
imc-dispatcher-5fc7ccf7d8-729ds        1/1     Running   0          4m32s
```

If not, you can follow the instructions on Knative Eventing Installation [page](https://knative.dev/docs/eventing/getting-started/#installing-knative-eventing).

## Broker

We need to inject a Broker in the namespace where we want to receive messages.
Let's use the default namespace.

```bash
kubectl label namespace default knative-eventing-injection=enabled
```

You should see a Broker in the namespace:

```bash
kubectl get broker

NAME      READY   REASON   URL                                                     AGE
default   True             http://default-broker.default.svc.cluster.local   55s
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

### Knative Service

You can have any kind of addressable as event sinks (Kubernetes Service, Knative Service etc.). For this part, let's use a Kubernetes Service.

Create a [kservice.yaml](../eventing/helloworld/kservice.yaml).

```bash
kubectl apply -f kservice.yaml
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

Create a [trigger.yaml](../eventing/helloworld/trigger.yaml).

Notice that we're filtering with the required attribute `type` with value `event-display`. Only messages with this attribute will be sent to the `event-display` service.

Create the trigger:

```bash
kubectl apply -f trigger.yaml

trigger.eventing.knative.dev/trigger-event-display created
```

Check that the trigger is ready:

```bash
kubectl get trigger

NAME                    READY   REASON   BROKER    SUBSCRIBER_URI                                          AGE
trigger-event-display   True             default   http://event-display.defualt.svc.cluster.local/   23s
```

## Producer

You can only access the Broker from within your Eventing cluster. Normally, you would create a Pod within that cluster to act as your event producer. In this case, we'll simply create a Pod with curl installed and use curl to manually send messages.

### Curl Pod

Create a [curl-pod.yaml](../eventing/helloworld/curl-pod.yaml) file.

Create the pod:

```bash
kubectl apply -f curl-pod.yaml

pod/curl created
```

### Send events to Broker

SSH into the pod:

```bash
kubectl attach curl -it

Defaulting container name to curl.
Use 'kubectl describe pod/' to see all of the containers in this pod.
If you don't see a command prompt, try pressing enter.
[ root@curl:/ ]$
```

Send the event. Notice that we're sending with event type `event-display`:

```bash
curl -v "http://default-broker.default.svc.cluster.local" \
  -X POST \
  -H "Ce-Id: say-hello" \
  -H "Ce-Specversion: 1.0" \
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
kubectl logs event-display-84485c6d9d-ttfp9

info: event_display.Startup[0]
      Event Display received event: {"msg":"Hello Knative1!"}
```

If you send another message without `event-display` type, that won't trigger the Event Display. 
