# Event registry

Knative comes with a simple [event registry](https://knative.dev/docs/eventing/event-registry/). Using the registry, you can discover the different types of events you can consume from the Broker.

Note that event registry only registers events going through a Broker. To check for registered events in a namespace (in this case `default` namespace):

```bash
kubectl get eventtypes -n default

No resources found.
```

## Create Event source

Let's create an Event source with a Broker to see events being registered. If you haven't already, label the default namespace to get a Broker injected:

```bash
kubectl label namespace default knative-eventing-injection=enabled
```

And check that Broker is injected:

```bash
kubectl get broker

NAME      READY   REASON   URL
default   True             http://default-broker.default.svc.cluster.local
```

*Note:* If your environment doesn't support automatic injection, refer to [Broker](broker.md).

Now, create a PingSource event source. You can use the [source-broker.yaml](../eventing/ping/source-broker.yaml) from [ScheduledEventing](scheduledeventing.md).

```bash
kubectl apply -f source-broker.yaml

cronjobsource.sources.eventing.knative.dev/test-cronjob-source-broker created
```

## Check Event registry

Now, if you check the event registry, you should see CronJob event registered as an event type:

```bash
kubectl get eventtypes -n default


NAME                TYPE                        SOURCE
dev.knative.cronjob.event-aeccfcfc-15d5-11ea-8292-42010a840008   dev.knative.cronjob.event
```
