# Hello World Build

[Knative Build](https://github.com/knative/build) utilizes existing Kubernetes primitives to provide you with the ability to run on-cluster container builds from source. For example, you can write a build that uses Kubernetes-native resources to obtain your source code from a repository, build a container image, then run that image.

The Knative Build API has a [Build](https://github.com/knative/docs/blob/master/build/builds.md) type that represents a single build job with one or more steps. There's also a [BuildTemplate](https://github.com/knative/docs/blob/master/build/build-templates.md) that allows you to chain Builds and parameterize them. 

In the previous labs, we've been building and pushing container images manually to DockerHub. Let's utilize Knative Build and push to Google Container Registry (GCR) instead. 

## Design the build

[Kaniko](https://github.com/GoogleContainerTools/kaniko) is a tool to build container images from a Dockerfile, inside a container in Kubernetes cluster. The advantage is that Kaniko doesn't depend on a Docker daemon. We'll use Kaniko in our Build step. 

Create a [build-helloworld-csharp-gcr.yaml](../build/build-helloworld-csharp-gcr.yaml) build file:

```yaml
apiVersion: build.knative.dev/v1alpha1
kind: Build
metadata:
  name: build-helloworld-csharp-gcr
spec:
  source:
    git:
      url: "https://github.com/knative/docs.git"
      revision: "v0.1.x"
    subPath: "serving/samples/helloworld-csharp/"
  steps:
  - name: build-and-push
    image: "gcr.io/kaniko-project/executor:v0.6.0"
    args:
    - "--dockerfile=/workspace/Dockerfile"
    # MY_GCP_PROJECT: Replace with the GCP Project's ID.
    - "--destination=gcr.io/MY_GCP_PROJECT/helloworld-csharp:knativebuild"
```
This uses Knative Build to download the source code in the 'workspace' directory and then use Kaniko to build and push an image to GCR.

## Run and watch the build

You can start the build with:

```bash
kubectl apply -f build-helloworld-csharp-gcr.yaml
```
Check that it is created:

```bash
kubectl get build
```

Soon after, you'll see a pod created for the build:

```bash
kubectl get pods
NAME                                             READY     STATUS    
build-helloworld-csharp-gcr-pod-454bd8           0/1       Init:2/3
```
You can see the progress of the build with:

```bash
kubectl logs --follow --container=build-step-build-and-push build-helloworld-csharp-gcr-pod-454bd8
```
When the build is finished, you'll see the pod in `Completed` state:

```bash
kubectl get pods
NAME                                              READY     STATUS 
build-helloworld-csharp-gcr-pod-454bd8            0/1       Completed
```
At this point, you should see the image pushed to GCR:

![Google Container Registry](./images/gcr.png)

## What's Next?
[Docker Hub Build](10-dockerbuild.md)
