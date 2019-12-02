# Scheduled service

In this sample, we'll take a look at how to call a service on a schedule with CronJob event source in Knative Eventing.

## Consumer

For the event consumer, we can use the Event Display service in [Hello World Eventing](helloworldeventing.md) sample. Go through the steps mentioned there to create and deploy the Event Display service. 

## CronJob event source

Create a [cronjob-source.yaml](../eventing/cronjob-source.yaml) in the same namespace (in this case `default`) as your service. 

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

In this case, we'll be calling the Event Display service every minute. 

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

## What's Next?

[Integrate with Translation API](translationeventing.md)
