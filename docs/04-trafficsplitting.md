# Traffic Splitting

So far, whenever we have a configuration change (code or env variables), we apply that change which in turn creates a new revision and the route is updated to direct 100% of the traffic to the new revision. This is because we've been using `spec.runLatest` mode in our service definition.

What if you want to control how much traffic the new revision gets? You can do that by using `spec.release` mode in the service definition.

## Split traffic between old and new 

Let's create a new revision `v4` that gets only 20% of the traffic while the old revision `v1` gets 80%. 

Create a [service-v4.yaml](../serving/helloworld-csharp/service-v4.yaml) file that has `TARGET` value of `C# Sample v4` and it uses `release` mode:

```yaml
apiVersion: serving.knative.dev/v1alpha1
kind: Service
metadata:
  name: helloworld-csharp
  namespace: default
spec:
  release:
    # Ordered list of 1 or 2 revisions. 
    # First revision is traffic target "current"
    # Second revision is traffic target "candidate"
    revisions: ["helloworld-csharp-00001", "helloworld-csharp-00004"]
    rolloutPercent: 20 # Percent [0-99] of traffic to route to "candidate" revision
    configuration:
      revisionTemplate:
        spec:
          container:
            # Replace meteatamel with your actual DockerHub
            image: docker.io/meteatamel/helloworld-csharp:v1
            env:
              - name: TARGET
                value: "C# Sample v4"
```

Note that the release mode lists two `revisions` (current and candidate) and `rolloutPercent` defines how much traffic the candidate (in this case `v4`) will receive

Apply the change:

```bash
kubectl apply -f service-v4.yaml
```
You should first see a new revision is created for `v4`:

```bash
kubectl get revision                             
NAME                      
helloworld-csharp-00001   
helloworld-csharp-00002   
helloworld-csharp-00003   
helloworld-csharp-00004   
```
You should see roughly 20% of the requests going to the new revision:

```bash
for i in {1..10}; do curl "http://helloworld-csharp.default.$KNATIVE_INGRESS.nip.io" ; sleep 1; done
Hello C# Sample v1
Hello C# Sample v1
Hello C# Sample v1
Hello C# Sample v4
```

## Split traffic between existing revisions 

What if you want to split traffic with existing revisions? You can do that by referring to the existing revisions in the `revision` field.  

Create a [service-v5.yaml](../serving/helloworld-csharp/service-v5.yaml) file that refers to `v1` and `v3` revisions that are split by 50%:

```yaml
apiVersion: serving.knative.dev/v1alpha1
kind: Service
metadata:
  name: helloworld-csharp
  namespace: default
spec:
  release:
    # Ordered list of 1 or 2 revisions. 
    # First revision is traffic target "current"
    # Second revision is traffic target "candidate"
    revisions: ["helloworld-csharp-00001", "helloworld-csharp-00003"]
    rolloutPercent: 50 # Percent [0-99] of traffic to route to "candidate" revision
    configuration:
      revisionTemplate:
        spec:
          container:
            # Replace meteatamel with your actual DockerHub
            image: docker.io/meteatamel/helloworld-csharp:v1
            env:
              - name: TARGET
                value: "C# Sample v4"
```
Apply the change:

```bash
kubectl apply -f service-v5.yaml
```
You should see roughly 50% of the requests split between revisions:

```bash
for i in {1..10}; do curl "http://helloworld-csharp.default.$KNATIVE_INGRESS.nip.io" ; sleep 1; done
Hello C# Sample v1
Bye C# Sample v3
Hello C# Sample v1
Bye C# Sample v3
```

## What's Next?
[Configure autoscaling](04.5-configureautoscaling.md)