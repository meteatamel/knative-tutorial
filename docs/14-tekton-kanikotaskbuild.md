# Kaniko Task Build

In the [previous lab](12-tekton-helloworldbuild.md), we created a Task to invoke Kaniko with all the required parameters for Kaniko and then we created a TaskRun to invoke that Task. This works but a better approach is to use a Task from the [Tekton Catalog](https://github.com/tektoncd/catalog) which is a repository of ready to use Tasks and Pipelines. 

In this lab, we will use [Kaniko Task](https://github.com/tektoncd/catalog/tree/master/kaniko).

## Install Kaniko Task

First, we need to install Kaniko Task:

```bash
kubectl apply -f https://raw.githubusercontent.com/tektoncd/catalog/master/kaniko/kaniko.yaml
```

Check that it is installed:

```bash
kubectl get task

NAME     AGE
kaniko   45m
```

## Design the TaskRun

Let's create a TaskRun now. Create a [taskrun-build-kaniko-helloworld-gcr.yaml](../build/taskrun-build-kaniko-helloworld-gcr.yaml) file:

```yaml
apiVersion: tekton.dev/v1alpha1
kind: TaskRun
metadata:
  name: build-kaniko-helloworld-gcr
spec:
  taskRef:
    name: kaniko
  inputs:
    resources:
    - name: source
      resourceSpec:
        type: git
        params:
        - name: url
          value: https://github.com/meteatamel/knative-tutorial
    params:
    - name: DOCKERFILE
      value: Dockerfile
    - name: CONTEXT
      value: serving/helloworld/csharp
  outputs:
    resources:
    - name: image
      resourceSpec:
        type: image
        params:
        - name: url
          # Replace {PROJECT_ID} with your GCP Project's ID.
          value: gcr.io/{PROJECT_ID}/helloworld:kaniko-tekton
```

Notice how the TaskRun is simply using the Kaniko Task and supplying the required parameters. 

## Run and watch the TaskRun

Start the TaskRun:

```bash
kubectl apply -f taskrun-build-kaniko-helloworld-gcr.yaml
```

After a few minutes, check the build is succeeded:

```bash
kubectl get taskrun

NAME                          SUCCEEDED
build-kaniko-helloworld-gcr   True
```

At this point, you should see the image pushed to GCR.

## What's Next?

[Buildpacks Build Template](14-buildpacksbuildtemplate.md)
