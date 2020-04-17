# Cluster local services

So far, we've been deploying publicly accessible services. In this lab, we'll create a private/cluster-local service that is not accessible from outside the cluster. You can read more on private cluster-local services in the [docs](https://knative.dev/docs/serving/cluster-local-route/).

## Deploy the private Knative service

Create a [service-local.yaml](../serving/helloworld/service-local.yaml) file.

Notice how we labeled our service with `cluster-local`. This makes the service private.

Deploy the service:

```bash
kubectl apply -f service-local.yaml
```

## Check that service is local

You can check that service is private:

```bash
kubectl get ksvc helloworld-local

NAME               URL
helloworld-local   http://helloworld-local.default.svc.cluster.local
```
Notice that the URL has `svc.cluster.local` (and not the xip.io domain) in it which makes it not publicly accessible.

## Turn a public service into local

You can also take an existing public service and turn into a local service by simply adding the label.

For example, deploy the first version of helloworld service:

```bash
kubectl apply -f service-v1.yaml
```
You should be able to access it via curl because it's public:

```bash
curl http://helloworld.default.$ISTIO_INGRESS.xip.io

Hello v1
```

Label the service with `cluster-local`:

```bash
kubectl label kservice helloworld serving.knative.dev/visibility=cluster-local

service.serving.knative.dev/helloworld labeled
```

The service now is local and you cannot curl it with the public URL. 

To make it public again, you can remove the label:

```bash
kubectl label kservice helloworld serving.knative.dev/visibility-

service.serving.knative.dev/helloworld labeled
```
And, curl should work again now. 

