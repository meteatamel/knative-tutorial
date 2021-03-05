# Docker Hub Build

In the [previous lab](tekton-helloworldbuild.md), we built and pushed a container image to Google Cloud Registry (GCR). In this lab, we will push to Docker Hub instead. It's more involved as we need to register secrets for Docker Hub.

## Register secrets for Docker Hub

We need to first register a secret in Kubernetes for authentication with Docker Hub.

Create a [docker-secret.yaml](../build/docker-secret.yaml) file for `Secret` manifest, which is used to store your Docker Hub credentials.

Make sure to replace `BASE64_ENCODED_USERNAME` and `BASE64_ENCODED_PASSWORD` with your Base64 encoded DockerHub username and password.

Create a [service-account.yaml](../build/service-account.yaml) for `Service Account` used to link the build process to the secret.

Apply the `Secret` and `Service Account`:

```bash
kubectl apply -f docker-secret.yaml
kubectl apply -f service-account.yaml
```

## Design the TaskRun

Create a [taskrun-build-helloworld-docker.yaml](../build/taskrun-build-helloworld-docker.yaml) file.

This is very similar to before, except we're using `build-bot` as `serviceAccountName` to authenticate with DockerHub.

## Run and watch the TaskRun

You can start the build with:

```bash
kubectl apply -f taskrun-build-helloworld-docker.yaml

pipelineresource.tekton.dev/git-knative-tutorial unchanged
pipelineresource.tekton.dev/image-docker-knative-tutorial created
taskrun.tekton.dev/build-helloworld-docker created
```

Check that all the Tekton artifacts are created:

```bash
kubectl get tekton-pipelines
```

Soon after, you'll see a pod created for the build:

```bash
kubectl get pods

NAME                                     READY     STATUS
build-helloworld-docker-pod-a1d405        0/4       Init:2/3
```

You can see the progress of the build with:

```bash
kubectl logs --follow --container=step-build-and-push <podid>
```

When the build is finished, you'll see the pod in `Completed` state:

```bash
kubectl get pods

NAME                                     READY     STATUS
build-helloworld-docker-pod-a1d405        0/4       Completed
```

At this point, you should see the image pushed to Docker Hub:

![Docker Hub](./images/dockerhub.png)