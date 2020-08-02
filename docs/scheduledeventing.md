# Scheduled service

In this sample, we'll take a look at how to call a service on a schedule in Knative Eventing.

## Consumer

For the event consumer, we can use the Event Display service in [Hello World Eventing](helloworldeventing.md) sample. Go through the steps mentioned there to create and deploy the Event Display service.

## Ping event source - Service sink

Create a [source.yaml](../eventing/ping/source.yaml) in the same namespace (in this case `default`) as your service.

In this case, we have the service as the sink. We'll be calling the Event Display service every minute.

Create the `PingSource`:

```bash
kubectl apply -f source.yaml
```

## Test the service

We can see that the service is triggered every minute in the service logs:

```bash
kubectl logs --follow <podid>

Event Display received event: {"message":"Hello world from ping!"}
```

# Broker

You can also inject a Broker in the namespace where we want to receive messages. Let's use the default namespace:

```bash
kubectl label ns default eventing.knative.dev/injection=enabled

namespace/default labeled
```

In a few seconds, you should see a Broker in the default namespace:

```bash
kubectl get broker

NAME      READY   REASON   URL
default   True             http://broker-ingress.knative-eventing.svc.cluster.local/default/default
```

*Note:* If your environment doesn't support automatic injection, refer to [Broker](broker.md).

## Event source - Broker sink

Next, setup `PingSource` to point to our Broker instead.

Create a [source-broker.yaml](../eventing/ping/source-broker.yaml).

In this case, we have the Broker in the default namespace as the sink.

Create the event source:

```bash
kubectl apply -f source-broker.yaml
```

You need to create a [trigger.yaml](../eventing/ping/trigger.yaml) to listen for events.

Notice that we're filtering on Ping events.

```bash
kubectl apply -f trigger.yaml
```

At this point, you should see scheduled events in the Event Display.
