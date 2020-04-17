# Configure Autoscaling

You might have realized that autoscaler in Knative scales down pods to zero after some time. This is actually configurable through annotations. The type of autoscaler itself is also configurable. [Autoscale Sample](https://knative.dev/docs/serving/samples/autoscale-go/index.html) in Knative docs explains the details of autoscaler but let's recap the main points here as well.

There are two autoscaler classes built into Knative:

1. The default concurrency-based autoscaler which is based on the average number of in-flight requests per pod.
2. Kubernetes CPU-based autoscaler which autoscales on CPU usage.

The autoscaling can be bounded with `minScale` and `maxScale` annotations.

## Create a 'Sleeping' service

Let's deploy a service to showcase autoscaling, by essentially sleeping for 4,000ms before responding to requests:

- C# [Startup.cs](../serving/sleepingservice/csharp/Startup.cs)

  ```csharp
  app.Run(async (context) =>
  {
      Thread.Sleep(4000);
      await context.Response.WriteAsync("Hello World!");
  });
  ```

- Python [app.py](../serving/sleepingservice/python/app.py)

  ```python
  @app.route('/')
  def hello_world():
      sleep(4)
      return 'Hello World!'
  ```

## Build and push Docker image

In [sleepingservice](../serving/sleepingservice/) folder, go to the folder for the language of your choice ([csharp](../serving/sleepingservice/csharp/), [python](../serving/sleepingservice/python/)). In that folder, build and push the container image. Replace `{username}` with your DockerHub username:

```bash
docker build -t {username}/sleepingservice:v1 .

docker push {username}/sleepingservice:v1
```

## Configure autoscaling

Take a look at the [service.yaml](../serving/sleepingservice/service.yaml) file.

Note the autoscaling annotations. We're keeping the default concurrency based autoscaling but setting the `target` to 1, so we can showcase autoscaling. We're also setting `minScale` to 1 and `maxScale` to 5. This will make sure that there is a single pod at all times and no more than 5 pods.

Create the service:

```bash
kubectl apply -f service.yaml
```

Check that pod for the service is running:

```bash
kubectl get pods

NAME
sleepingservice-00001-deployment-5865bc498c-w7qc7
```

And this pod will continue to run, even if there's no traffic.

## Test autoscaling

Let's now send some traffic to our service to see that it scales up. Download and install [Fortio](https://github.com/fortio/fortio) if you don't have it.

Send some requests to our sleeping service:

```bash
fortio load -t 0 http://sleepingservice.default.$ISTIO_INGRESS.xip.io
```

After a while, you should see pods scaling up to 5:

```bash
kubectl get pods

sleepingservice-cphdq-deployment-5bf8ddb477-787sq
sleepingservice-cphdq-deployment-5bf8ddb477-b6ms9
sleepingservice-cphdq-deployment-5bf8ddb477-bmrds
sleepingservice-cphdq-deployment-5bf8ddb477-g2ssv
sleepingservice-cphdq-deployment-5bf8ddb477-kzt5t
```

Once you kill Fortio, you should also see the sleeping service scale down to 1!
