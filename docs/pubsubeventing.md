# Cloud Pub/Sub triggered service

In this sample, we'll take a look at how to connect GCP Pub/Sub messages to a service with Knative Eventing. We'll roughly be following [CloudPubSubSource Example](https://github.com/google/knative-gcp/blob/master/docs/examples/cloudpubsubsource/README.md) docs page.

## Knative with GCP & PubSub Topic

We're assuming that you already went through [Install Knative with GCP](../setup/README.md) section of the setup.

## Create a Pub/Sub topic

Create a Pub/Sub topic where messages will be sent:

```bash
gcloud pubsub topics create testing
```

## CloudPubSubSource

Create a CloudPubSubSource to connect PubSub messages to Knative Eventing. The default [cloudpubsubsource.yaml](https://github.com/google/knative-gcp/blob/master/docs/examples/cloudpubsubsource/cloudpubsubsource.yaml) connects Pub/Sub messages to a service directly.

Instead, use the following [cloudpubsubsource.yaml](../eventing/pubsub/cloudpubsubsource.yaml) to connect Pub/Sub messages to a Broker, so, we can have multiple triggers to invoke multiple services on the same message.

Note that there's an alternative
[cloudpubsubsource-workload.yaml](../eventing/pubsub/cloudpubsubsource-workload.yaml).
Use this version instead if you setup workload identity on GKE.

Create the CloudPubSubSource:

```bash
kubectl apply -f cloudpubsubsource.yaml
```

## Broker

If there's no Broker in the default namespace already, label the namespace:

```bash
kubectl label ns default eventing.knative.dev/injection=enabled
```

You should see a Broker in the namespace:

```bash
kubectl get broker

NAME      READY   REASON   URL                                               AGE
default   True             http://default-broker.default.svc.cluster.local   52m
```

## Consumer

For the event consumer, we can use the Event Display service defined in
[kservice.yaml](../eventing/pubsub/kservice.yaml).

Create the service:

```bash
kubectl apply -f kservice.yaml
```

## Trigger

Connect the Event Display service to the Broker with a Trigger defined in [trigger.yaml](../eventing/pubsub/trigger.yaml):

```bash
kubectl apply -f trigger.yaml
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

Inspect the logs of the pod (replace `<podid>` with actual pod id):

```bash
kubectl logs <podid> -c user-container --follow
```

You should see something similar to this:

```text
info: event_display.Startup[0]
      Received CloudEvent
      ID: 1203138894276445
      Source: //pubsub.googleapis.com/projects/knative-atamel/topics/testing
      Type: com.google.cloud.pubsub.topic.publish
      Subject:
      DataSchema:
      DataContentType: application/json
      Time: 2020-05-13T14:57:20.106Z
      SpecVersion: V1_0
      Data: {"subscription":"cre-pull-b1d2ed86-d4ed-4cea-919e-39895b1f818d","message":{"messageId":"1203138894276445","data":"SGVsbG8gV29ybGQ=","publishTime":"2020-05-13T14:57:20.106Z"}}
```
