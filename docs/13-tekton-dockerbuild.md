# Docker Hub Build

In the [previous lab](12-tekton-helloworldbuild.md), we built and pushed a container image to Google Cloud Registry (GCR). In this lab, we will push to Docker Hub instead. It's more involved as we need to register secrets for Docker Hub.

## Register secrets for Docker Hub

We need to first register a secret in Kubernetes for authentication with Docker Hub.

Create a [docker-secret.yaml](../build/docker-secret.yaml) file for `Secret` manifest, which is used to store your Docker Hub credentials:

```yaml
apiVersion: v1
kind: Secret
metadata:
  name: basic-user-pass
  annotations:
    tekton.dev/docker-0: https://index.docker.io/v1/
type: kubernetes.io/basic-auth
data:
  # Use 'echo -n "username" | base64' to generate this string
  username: BASE64_ENCODED_USERNAME
  # Use 'echo -n "password" | base64' to generate this string
  password: BASE64_ENCODED_PASSWORD
```

Make sure to replace `BASE64_ENCODED_USERNAME` and `BASE64_ENCODED_PASSWORD` with your Base64 encoded DockerHub username and password.

Create a [service-account.yaml](../build/service-account.yaml) for `Service Account` used to link the build process to the secret:

```yaml
apiVersion: v1
kind: ServiceAccount
metadata:
  name: build-bot
secrets:
  - name: basic-user-pass
```

Apply the `Secret` and `Service Account`:

```bash
kubectl apply -f docker-secret.yaml
secret "basic-user-pass" created

kubectl apply -f service-account.yaml
serviceaccount "build-bot" created
```

## Design the TaskRun

We will use [Kaniko](https://github.com/GoogleContainerTools/kaniko) again. Create a [taskrun-build-helloworld-docker.yaml](../build/taskrun-build-helloworld-docker.yaml) file:

```yaml
apiVersion: tekton.dev/v1alpha1
kind: PipelineResource
metadata:
  name: git-knative-tutorial
spec:
  type: git
  params:
    - name: revision
      value: master
    - name: url
      # Point to Git url
      value: https://github.com/meteatamel/knative-tutorial
---
apiVersion: tekton.dev/v1alpha1
kind: PipelineResource
metadata:
  name: image-docker-knative-tutorial
spec:
  type: image
  params:
    - name: url
      # Replace {username} with your actual DockerHub
      value: docker.io/{username}/helloworld:tekton
---
apiVersion: tekton.dev/v1alpha1
kind: TaskRun
metadata:
  name: build-helloworld-docker
spec:
  serviceAccount: build-bot
  taskRef:
    name: build-docker-image-from-git-source
  inputs:
    resources:
      - name: docker-source
        resourceRef:
          name: git-knative-tutorial
    params:
      - name: pathToDockerFile
        value: Dockerfile
      - name: pathToContext
        # Point to Dockerfile
        value: /workspace/docker-source/serving/helloworld/csharp 
  outputs:
    resources:
      - name: builtImage
        resourceRef:
          name: image-docker-knative-tutorial
```

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

## What's Next?

[Kaniko Task Build](14-tekton-kanikotaskbuild.md)