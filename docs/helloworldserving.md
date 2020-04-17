# Hello World Serving

[Knative Serving](https://www.knative.dev/docs/serving/) already has a [samples](https://www.knative.dev/docs/serving/samples/) page with `Hello World` services for various languages. In this tutorial, we will build the same with a couple of minor differences:

1. When building the Docker image, we tag our images with `v1`, `v2`, etc. to better track different versions of the service.

2. When defining the service, we use `service-v1.yaml` for the first version, `service-v2.yaml` for the second version etc. Again, to keep track of different service configurations better.

## Create a 'Hello World' service

You can either create your 'Hello World' service as described in Knative Serving or take a look at the services we already created in [helloworld](../serving/helloworld/) folder for various languages ([csharp](../serving/helloworld/csharp/), [python](../serving/helloworld/python/)).

## Build and push Docker image

In folder of your language of choice, build and push the container image defined by `Dockerfile`. Replace `{username}` with your DockerHub username:

```bash
docker build -t {username}/helloworld:v1 .

docker push {username}/helloworld:v1
```

## Deploy the Knative service

Take a look at [service-v1.yaml](../serving/helloworld/service-v1.yaml) file where we define a Knative service.

After the container is pushed, deploy the Knative service:

```bash
kubectl apply -f service-v1.yaml
```

Check that pods are created and all Knative constructs (service, configuration, revision, route) have been deployed:

```bash
kubectl get pod,ksvc,configuration,revision,route

NAME                                                      READY     STATUS    RESTARTS
pod/helloworld-c4pmt-deployment-7fdb5c5dc9-wf2bp   3/3       Running   0

NAME
service.serving.knative.dev/helloworld

NAME
configuration.serving.knative.dev/helloworld

NAME
revision.serving.knative.dev/helloworld-00001

NAME
route.serving.knative.dev/helloworld
```

## (Optional) Install watch

[Watch](https://en.wikipedia.org/wiki/Watch_(Unix)) is a command-line tool that runs the specified command repeatedly and displays the results on standard output so you can watch it change over time. It can be useful to watch Knative pods, services, etc. If you're using Mac, you can install watch using `homebrew` or a similar tool.

To watch pods, Knative service, configuration, revision and route using watch, you can do this:

```bash
watch -n 1 kubectl get pod,ksvc,configuration,revision,route

Every 1.0s: kubectl get pod,ksvc,config...

NAME                                                     READY     STATUS    RESTARTS   AGE
pod/helloworld-00001-deployment-b6c485d9f-rm7l4   3/3       Running   0          1m

NAME                                     AGE
service.serving.knative.dev/helloworld   1m

NAME                                           AGE
configuration.serving.knative.dev/helloworld   1m

NAME                                            AGE
revision.serving.knative.dev/helloworld-c4pmt   1m

NAME                                   AGE
route.serving.knative.dev/helloworld   1m
```

## Test the service

Starting with v0.13.0, Knative Serving comes up with an optional Kubernetes job
that setups up xip.io as the default domain. This is useful for demo purposes
and we're assuming that you have it setup. You can check the installation
instructions on [Knative
Installation](https://knative.dev/docs/install/any-kubernetes-cluster/) page or
use the [install-serving](../setup/install-serving) script we provided.

To test the service, we need to find the IP address of the Istio ingress gateway and the URL of the service.

The IP address of Istio ingress is listed under `EXTERNAL_IP`:

```bash
kubectl get svc istio-ingressgateway -n istio-system
```

Let's set this `EXTERNAL_IP` to an `ISTIO_INGRESS` variable:

```bash
export ISTIO_INGRESS=$(kubectl -n istio-system get service istio-ingressgateway -o jsonpath='{.status.loadBalancer.ingress[0].ip}')
```

The URL of the service follows this format: `{service}.{namespace}.example.com`. In this case, we have `helloworld.default.example.com`.

Make a request to your service:

```bash
curl http://helloworld.default.$ISTIO_INGRESS.xip.io

Hello v1
```
