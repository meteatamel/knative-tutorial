# Traffic Splitting

So far, whenever we have a configuration change (code or env variable), we apply that change which in turn creates a new revision and the route is updated to direct 100% of the traffic to the new revision.

What if you want to control how the traffic is routed between current and latest revision? You can do that in Knative.

## Named revisions and pinning to a revision

We deployed three versions of our Knative that created 3 revisions with random names that you can verify:

```bash
kubectl get revision

NAME               SERVICE NAME
helloworld-z9clz   helloworld-z9clz
helloworld-f4xvr   helloworld-f4xvr
helloworld-ln8rv   helloworld-ln8rv
```

For traffic splitting, it's useful to have meaningful revision names. It's also useful to pin the traffic to a certain revision. Let's do both.

Create a [service-v1-pinned.yaml](../serving/helloworld/service-v1-pinned.yaml) file as follows:

```yaml
apiVersion: serving.knative.dev/v1alpha1
kind: Service
metadata:
  name: helloworld
  namespace: default
spec:
  template:
    metadata:
      name: helloworld-v1
    spec:
      containers:
        # Replace {username} with your actual DockerHub
        - image: docker.io/{username}/helloworld:v1
          env:
            - name: TARGET
              value: "v1"
  traffic:
  - tag: current
    revisionName: helloworld-v1
    percent: 100
  - tag: latest
    latestRevision: true
    percent: 0
```

Notice a couple of things:

1. The revision of the Service has now a specific name: `helloworld-v1`
2. There's a `traffic` section where we pin 100% of the traffic to the named revision.

In this setup, we can still deploy a new revision but that revision (`latest`) is not going to get the traffic.

Apply the change:

```bash
kubectl apply -f service-v1-pinned.yaml
```

You should see the new named revision created:

```bash
kubectl get revision

NAME               SERVICE NAME       GENERATION
helloworld-z9clz   helloworld-z9clz   1
helloworld-f4xvr   helloworld-f4xvr   2
helloworld-ln8rv   helloworld-ln8rv   3
helloworld-v1      helloworld-v1      4
```

And `helloworld-v1` is the one getting the traffic:

```bash
curl http://helloworld.default.$ISTIO_INGRESS.xip.io

Hello v1
```

## Deploy a new version

Let's create a new revision. Create a [service-v4.yaml](../serving/helloworld/service-v4.yaml) file that has `TARGET` value of `v4`:

```yaml
apiVersion: serving.knative.dev/v1alpha1
kind: Service
metadata:
  name: helloworld
  namespace: default
spec:
  template:
    metadata:
      name: helloworld-v4
    spec:
      containers:
        # Replace {username} with your actual DockerHub
        - image: docker.io/{username}/helloworld:v1
          env:
            - name: TARGET
              value: "v4"
  traffic:
  - tag: current
    revisionName: helloworld-v1
    percent: 100
  - tag: latest
    latestRevision: true
    percent: 0
```

Notice that even though a new revision is being created `helloworld-v4`, the old revision `helloworld-v1` is the one getting the traffic.

Apply the change:

```bash
kubectl apply -f service-v4.yaml
```

You should see the new named revision created:

```bash
kubectl get revision

NAME               SERVICE NAME       GENERATION
helloworld-z9clz   helloworld-z9clz   1
helloworld-f4xvr   helloworld-f4xvr   2
helloworld-ln8rv   helloworld-ln8rv   3
helloworld-v1      helloworld-v1      4
helloworld-v4      helloworld-v4      5
```

But `helloworld-v1` is still one getting the traffic:

```bash
curl http://helloworld.default.$ISTIO_INGRESS.xip.io

Hello v1
```

You can verify that the new version is deployed by accessing the `latest` endpoint:

```bash
curl http://latest-helloworld.default.$ISTIO_INGRESS.xip.io

Hello v4
```

But the `current` one is `helloworld-v1`:

```bash
curl http://current-helloworld.default.$ISTIO_INGRESS.xip.io

Hello v1
```

## Split the traffic 50-50

In this last section, let's split the traffic 50-50 between `helloworld-v1` and `helloworld-v4`. Create a [service-v1v4-split.yaml](../serving/helloworld/service-v1v4-split.yaml) file as follows:

```yaml
apiVersion: serving.knative.dev/v1alpha1
kind: Service
metadata:
  name: helloworld
  namespace: default
spec:
  template:
    metadata:
      name: helloworld-v4
    spec:
      containers:
        # Replace {username} with your actual DockerHub
        - image: docker.io/{username}/helloworld:v4
          env:
            - name: TARGET
              value: "v4"
  traffic:
  - tag: current
    revisionName: helloworld-v1
    percent: 50
  - tag: candidate
    revisionName: helloworld-v4
    percent: 50
  - tag: latest
    latestRevision: true
    percent: 0
```

Apply the change:

```bash
kubectl apply -f service-v1v4-split.yaml
```

You should see roughly 50% of the requests split between revisions:

```bash
for i in {1..10}; do curl http://helloworld.default.$ISTIO_INGRESS.xip.io; sleep 1; done
Hello v1
Hello v4
Hello v1
Hello v4
```
