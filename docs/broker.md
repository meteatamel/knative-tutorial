# Broker Creation

Some environments don't support the Broker injection in an automatic manner. If you are running this tutorial in an environment that was not created with the steps we have here, chances are you might have problems with Broker injection.

## Broker

To create your Broker on the default namespace, first define [broker.yaml](../eventing/broker/broker.yaml):

```bash
kubectl apply -f broker.yaml

broker.eventing.knative.dev/default created
```

Now you should see a Broker in the default namespace:

```bash
kubectl get broker

NAME      READY   REASON   URL
default   True             http://broker-ingress.knative-eventing.svc.cluster.local/default/default
```

There's also a default `InMemoryChannel` created and used by the Broker (but default channel can be [configured](https://knative.dev/docs/eventing/channels/default-channels/#setting-the-default-channel-configuration)):

```bash
kubectl get channel

NAME
inmemorychannel.messaging.knative.dev/default-kne-trigger
```
