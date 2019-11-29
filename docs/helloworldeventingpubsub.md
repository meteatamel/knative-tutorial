# Hello World Eventing with GCP Pub/Sub

## GCP PubSub event source

Follow the [GCP Cloud Pub/Sub source](https://knative.dev/docs/eventing/samples/gcp-pubsub-source/) docs page to set Knative Eventing with GCP Pub/Sub up until where you need to create an event display. We'll create our own event display and trigger to connect to it.  

## Update your install to use cluster local gateway

If you want to use Kubernetes Services as event sinks, you don't have to do anything extra. However, to have Knative Services as event sinks, you need to have them only visible within the cluster by adding Istio cluster local gateway as detailed [here](https://knative.dev/docs/install/installing-istio/#updating-your-install-to-use-cluster-local-gateway). 

Knative Serving comes with some yaml files to install cluster local gateway. 

First, you'd need to find the version of your Istio via something like this:

```bash
kubectl get pod istio-ingressgateway-f659695c4-lg8sm -n istio-system -oyaml | grep image

    image: gke.gcr.io/istio/proxyv2:1.1.16-gke.0
```

In this case, it's `1.1.16`. Then, you need to point to the Istio version close enough to your version under [third_party](https://github.com/knative/serving/tree/master/third_party) folder of Knative Serving. In this case, I used `1.3.5`:

```bash
kubectl apply -f https://raw.githubusercontent.com/knative/serving/master/third_party/istio-1.3.5/istio-knative-extras.yaml

serviceaccount/cluster-local-gateway-service-account created
serviceaccount/istio-multi configured
clusterrole.rbac.authorization.k8s.io/istio-reader configured
clusterrolebinding.rbac.authorization.k8s.io/istio-multi configured
service/cluster-local-gateway created
deployment.apps/cluster-local-gateway created
```

At this point, you can use Knative Services as event sinks in PullSubscription. 

## Create an Event Display

Follow the instructions for your preferred language to create a service to log out messages:

* [Create Event Display - C#](helloworldeventing-csharp.md)

* [Create Event Display - Python](helloworldeventing-python.md)

## Build and push Docker image

Build and push the Docker image (replace `{username}` with your actual DockerHub):

```bash
docker build -t {username}/event-display:v1 .

docker push {username}/event-display:v1
```

## Create Event Display

You can have any kind of addressable as event sinks in Knative eventing. In this case, we'll use a Knative Service as event sink. 

### Create Knative Service 

Create a [kservice.yaml](../eventing/event-display/kservice.yaml) file:

```yaml
apiVersion: serving.knative.dev/v1alpha1
kind: Service
metadata:
  name: event-display
  namespace: default
spec:
  template:
    spec:
      containers:
        # Replace {username} with your actual DockerHub
        - image: docker.io/{username}/event-display:v1
```

This defines a Knative Service to receive messages. 

Create the Event Display service:

```bash
kubectl apply -f kservice.yaml

service.serving.knative.dev/event-display created
```

## Create a trigger

Last but not least, we need connect Event Display service to Pub/Sub messages with a trigger. 

Create a [trigger-event-display-pubsub.yaml](../eventing/event-display/trigger-event-display-pubsub.yaml):

```yaml
apiVersion: eventing.knative.dev/v1alpha1
kind: Trigger
metadata:
  name: trigger-event-display
spec:
  subscriber:
    ref:
      apiVersion: serving.knative.dev/v1
      kind: Service
      name: event-display
```
This connects the messages from the broker to `event-display` service. 

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
kubectl logs --follow -c user-container <podid>
```

You should see something similar to this:

```text
Event Display received message: Hello World
```

## What's Next?

[Integrate with Translation API](translationeventing.md)
