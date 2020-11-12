# Broker Creation

You can create Broker in the namespace in 2 ways:

1. Automatically by labelling the namespace.
2. Manually.

## Automatically

Broker can be injected into a namespace by labelling it with
`eventing.knative.dev/injection=enabled` but this requires
Sugar Controller to be installed.

If you installed Knative Eventing with
instructions in [setup](setup), you have Sugar Controller installed:

```sh
kubectl get pods -n knative-eventing

sugar-controller-55c4fb657b-jrh52      1/1     Running
```

Create a Broker in the default namespace by labelling the namespace:

```sh
kubectl label ns default eventing.knative.dev/injection=enabled
```

## Manual

Create a Broker in the default namespace:

```sh
kubectl create -f - <<EOF
apiVersion: eventing.knative.dev/v1
kind: Broker
metadata:
  name: default
  namespace: default
EOF
```

## Check Broker

After creating the Broker, you should see a Broker in the default namespace:

```sh
kubectl get broker

NAME      READY   REASON   URL
default   True             http://broker-ingress.knative-eventing.svc.cluster.local/default/default
```

There's also a default `InMemoryChannel` created and used by the Broker (but default channel can be [configured](https://knative.dev/docs/eventing/channels/default-channels/#setting-the-default-channel-configuration)):

```sh
kubectl get channel

NAME
inmemorychannel.messaging.knative.dev/default-kne-trigger
```
