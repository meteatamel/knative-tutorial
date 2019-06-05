# Traffic Splitting

So far, whenever we have a configuration change (code or env variables), we apply that change which in turn creates a new revision and the route is updated to direct 100% of the traffic to the new revision. This is because we've been using `spec.runLatest` mode in our service definition.

What if you want to control how much traffic the new revision gets? You can do that by using `spec.release` mode in the service definition.

## Split traffic between old and new 

Let's create a new revision `v4` that gets only 20% of the traffic while the old revision `v1` gets 80%. 

Create a [service-v4.yaml](../serving/helloworld/service-v4.yaml) file that has `TARGET` value of `v4` and it uses `release` mode. Note that the revision names are not correct but we'll fix that later:

```yaml
apiVersion: serving.knative.dev/v1alpha1
kind: Service
metadata:
  name: helloworld
  namespace: default
spec:
  release:
    # Ordered list of 1 or 2 revisions. 
    # First revision is traffic target "current"
    # Second revision is traffic target "candidate"
    revisions: ["helloworld-00001", "helloworld-00004"]
    rolloutPercent: 20 # Percent [0-99] of traffic to route to "candidate" revision
    configuration:
      revisionTemplate:
        spec:
          container:
            # Replace {username} with your actual DockerHub
            image: docker.io/{username}/helloworld:v1
            env:
              - name: TARGET
                value: "v4"
```

The release mode lists two fake `revisions` (current and candidate) and `rolloutPercent` defines how much traffic the candidate (in this case `v4`) will receive

Apply the change:

```bash
kubectl apply -f service-v4.yaml
```
You should first see a new revision is created for `v4` (generation 4):

```bash
kubectl get revision 

NAME               SERVICE NAME               GENERATION
helloworld-4ht6f   helloworld-4ht6f-service   2
helloworld-8sv8s   helloworld-8sv8s-service   3
helloworld-t66v9   helloworld-t66v9-service   1
helloworld-zd5qk   helloworld-zd5qk-service   4
```

Now, replace the fake revision ids with the real ones for generation 1 and 4. The yaml file should look like this:

```yaml
apiVersion: serving.knative.dev/v1alpha1
kind: Service
metadata:
  name: helloworld
  namespace: default
spec:
  release:
    # Ordered list of 1 or 2 revisions. 
    # First revision is traffic target "current"
    # Second revision is traffic target "candidate"
    revisions: ["helloworld-t66v9", "helloworld-zd5qk"]
    rolloutPercent: 20 # Percent [0-99] of traffic to route to "candidate" revision
    configuration:
      revisionTemplate:
        spec:
          container:
            # Replace {username} with your actual DockerHub
            image: docker.io/{username}/helloworld:v1
            env:
              - name: TARGET
                value: "v4"
```
Apply the change:

```bash
kubectl apply -f service-v4.yaml
```

You should see roughly 20% of the requests going to the new revision:

```bash
for i in {1..10}; do curl "http://helloworld.default.$ISTIO_INGRESS.nip.io" ; sleep 1; done

Hello v1
Hello v1
Hello v1
Hello v4
```

## Split traffic between existing revisions 

What if you want to split traffic with existing revisions? You can do that by referring to the existing revisions in the `revision` field.  

Create a [service-v5.yaml](../serving/helloworld/service-v5.yaml) file that refers to `v1` and `v3` revisions that are split by 50%. Make sure you use the actual revision ids in your deployment:

```yaml
apiVersion: serving.knative.dev/v1alpha1
kind: Service
metadata:
  name: helloworld
  namespace: default
spec:
  release:
    # Ordered list of 1 or 2 revisions. 
    # First revision is traffic target "current"
    # Second revision is traffic target "candidate"
    revisions: ["helloworld-0001", "helloworld-0003"]
    rolloutPercent: 50 # Percent [0-99] of traffic to route to "candidate" revision
    configuration:
      revisionTemplate:
        spec:
          container:
            # Replace {username} with your actual DockerHub
            image: docker.io/{username}/helloworld-csharp:v1
            env:
              - name: TARGET
                value: "v4"
```
Apply the change:

```bash
kubectl apply -f service-v5.yaml
```
You should see roughly 50% of the requests split between revisions:

```bash
for i in {1..10}; do curl "http://helloworld.default.$ISTIO_INGRESS.nip.io" ; sleep 1; done
Hello v1
Bye v3
Hello v1
Bye v3
```

## What's Next?
[Configure autoscaling](05-configureautoscaling.md)