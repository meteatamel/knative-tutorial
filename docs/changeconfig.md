# Change Configuration

In [Knative Serving](https://github.com/knative/docs/tree/master/serving) whenever you change [Configuration](https://github.com/knative/serving/blob/master/docs/spec/spec.md#configuration) of the [Service](https://github.com/knative/serving/blob/master/docs/spec/spec.md#service), it creates a new [Revision](https://github.com/knative/serving/blob/master/docs/spec/spec.md#revision) which is a point-in-time snapshot of code. It also creates a new [Route](https://github.com/knative/serving/blob/master/docs/spec/spec.md#route) and the new Revision will start receiving traffic.

![Diagram](https://github.com/knative/serving/raw/master/docs/spec/images/object_model.png)

## Change environment variable

To see how Knative reacts to configuration changes, let's change the environment variable the container reads.

Create a [service-v2.yaml](../serving/helloworld/service-v2.yaml) file that changes `TARGET` value to `v2`.

Note that the image is still pointing to `v1` version. Apply the change:

```bash
kubectl apply -f service-v2.yaml
```

Now, you should see a new pod and a revision is created for the configuration change:

```bash
kubectl get pod,configuration,revision,route

NAME                                                      READY     STATUS    RESTARTS
pod/helloworld-c4pmt-deployment-7fdb5c5dc9-p2hr6   3/3       Running   0
pod/helloworld-vkvjt-deployment-7d7d9c9fdd-r27v8   3/3       Running   0

NAME                                                  configuration.serving.knative.dev/helloworld

NAME
revision.serving.knative.dev/helloworld-c4pmt
revision.serving.knative.dev/helloworld-vkvjt

NAME
route.serving.knative.dev/helloworld
```

Test that the route is also updated and prints out `v2`:

```bash
curl http://helloworld.default.$ISTIO_INGRESS.xip.io

Hello v2
```

## Change container image

Configuration changes are not limited to environment variables. For example, a new container image would also trigger a new revision and traffic routed to that revision.

To see this in action, change your main file to say 'Bye' instead of 'Hello':

- C# [Startup.cs](../serving/helloworld/csharp/Startup.cs)

  ```csharp
  await context.Response.WriteAsync($"Bye {target}\n");
  ```

- Python [app.py](../serving/helloworld/python/app.py)

  ```python
  return f'Bye {target}\n'
  ```

Build and push the Docker image tagging with `v3`. Replace `{username}` with your actual Docker Hub username:

```bash
docker build -t {username}/helloworld:v3 .

docker push {username}/helloworld:v3
```

Once the container image is pushed, create a [service-v3.yaml](../serving/helloworld/service-v3.yaml) file that changes `TARGET` value to `v3` but more importantly, it refers to the new image with tag `v3`.

Apply the change:

```bash
kubectl apply -f service-v3.yaml
```

Test that the route is updated to `v3` with the new container. It prints not only `v3` (from env variable) but also says Bye (from container):

```bash
curl http://helloworld.default.$ISTIO_INGRESS.xip.io
Bye v3
```
