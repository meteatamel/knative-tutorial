# Scheduled service

In this sample, we'll take a look at how to call a service on a schedule with CronJob event source in Knative Eventing.

## Consumer

For the event consumer, we can use the Event Display service in [Hello World Eventing](helloworldeventing.md) sample. Go through the steps mentioned there to create and deploy the Event Display service. 

## CronJob event source - Service sink

Create a [cronjob-source.yaml](../eventing/cronjob/cronjob-source.yaml) in the same namespace (in this case `default`) as your service. 

```yaml
apiVersion: sources.eventing.knative.dev/v1alpha1
kind: CronJobSource
metadata:
  name: test-cronjob-source
spec:
  schedule: "* * * * *"
  data: '{"message": "Hello world from cron!"}'
  sink:
    #apiVersion: serving.knative.dev/v1alpha1
    apiVersion: v1
    kind: Service
    name: event-display

```

In this case, we have the service as the sink. We'll be calling the Event Display service every minute. 

Create the CronJob event source:

```bash
kubectl apply -f cronjob-source.yaml

cronjobsource.sources.eventing.knative.dev/test-cronjob-source created
```

## Test the service

We can see that the service is triggered every minute in the service logs:

```bash
kubectl logs --follow <podid>

Event Display received event: {"message":"Hello world from cron!"}
```

## CronJob event source - Broker sink

You can also setup CronJob source to point to a Broker instead. 

Create a [cronjob-source-broker.yaml](../eventing/cronjob/cronjob-source-broker.yaml): 

```yaml
apiVersion: sources.eventing.knative.dev/v1alpha1
kind: CronJobSource
metadata:
  name: test-cronjob-source-broker
spec:
  schedule: "* * * * *"
  data: '{"message": "Hello world from cron!"}'
  sink:
    apiVersion: eventing.knative.dev/v1alpha1
    kind: Broker
    name: default
```

In this case, we have the Broker in the default namespace as the sink. 

Create the CronJob event source:

```bash
kubectl apply -f cronjob-source-broker.yaml

cronjobsource.sources.eventing.knative.dev/test-cronjob-source-broker created
```

You can need to create a Trigger to listen for CronJob source events:

```yaml
apiVersion: eventing.knative.dev/v1alpha1
kind: Trigger
metadata:
  name: trigger-event-display
spec:
  filter:
    attributes:
      type: dev.knative.cronjob.event
  subscriber:
    ref:
      #apiVersion: serving.knative.dev/v1
      apiVersion: v1
      kind: Service
      name: event-display
```

Notice that we're filtering on CronJob events. 

```bash
kubectl apply -f trigger-event-display-cron.yaml

trigger.eventing.knative.dev/trigger-event-display created
```

At this point, you should see CronJob events in the Event Display. 

