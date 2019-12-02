# Pub/Sub triggered service

In this sample, we'll take a look at how to connect GCP Pub/Sub messages to a service with Knative Eventing.

## GCP PubSub event source

Follow the [GCP Cloud Pub/Sub source](https://knative.dev/docs/eventing/samples/gcp-pubsub-source/) docs page to set Knative Eventing with GCP Pub/Sub up until where you need to create an event display. We'll create our own event display and trigger to connect to it.  

## Consumer

For the event consumer, we can use the Event Display service in [Hello World Eventing](helloworldeventing.md) sample. Go through the steps mentioned there to create and deploy the Event Display service. 

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

[Scheduled service](scheduledeventing.md)
