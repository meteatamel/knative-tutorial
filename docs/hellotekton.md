# Tekton Pipelines

Before Knative 0.8, [Knative Build](https://www.knative.dev/docs/build/) provided the ability to run on-cluster container builds from source. You could write a build that uses Kubernetes-native resources to obtain your source code from a repository, build a container image, then run that image.

As of Knative 0.8, [Knative Build](https://www.knative.dev/docs/build/) has been [deprecated](https://github.com/knative/build/issues/614) in favor of [Tekton Pipelines](https://github.com/tektoncd/pipeline). The Tekton Pipelines project provides Kubernetes resources for declaring CI/CD-style pipelines. Inspired by Knative Build, Tekton provides equivalent primitives of Knative Build and some more.

In this part of the tutorial, we'll take a look at how to build and push container images using Tekton Pipelines.

## Migrating from Knative Build

If you used Knative Build before, you might wonder how to migrate from Knative Build to Tekton. There's a [migration guide](https://github.com/tektoncd/pipeline/blob/master/docs/migrating-from-knative-build.md) with more details.

To recap here, this is the Tekton equivalents for Knative primitives:

| **Knative**          | **Tekton**  |
|----------------------|-------------|
| Build                | TaskRun     |
| BuildTemplate        | Task        |
| ClusterBuildTemplate | ClusterTask |

Additionally, the Tekton Catalog project (https://github.com/tektoncd/catalog) provides a catalog of re-usable tasks, similar to what Knative BuildTemplate repository did before.

## Install Tekton Pipelines

You can follow instructions at [Installing Tekton Pipelines](https://github.com/tektoncd/pipeline/blob/master/docs/install.md) to install Tekton.

As a recap, this is the command:

```sh
kubectl apply -f https://storage.googleapis.com/tekton-releases/pipeline/latest/release.yaml
```

Check that Tekton pipeline pods are running:

```sh
kubectl get pods -n tekton-pipelines

NAME                                           READY   STATUS
tekton-pipelines-controller-55c6b5b9f6-8p749   1/1     Running
tekton-pipelines-webhook-6794d5bcc8-pf5x7      1/1     Running
```

## Test Tekton

In Tekton, you start with defining a [Task](https://github.com/tektoncd/pipeline/blob/master/docs/tasks.md). Task defines the work that needs to be executed. Let's create a Hello World Task to make sure it is working as expected.

Create a [task-helloworld.yaml](../build/task-helloworld.yaml) file:

This is a simple task that will echo hello world. You can have one or more steps that are executed sequentially.

A [TaskRun](https://github.com/tektoncd/pipeline/blob/master/docs/taskruns.md) runs the Task you defined. Create a [taskrun-helloworld.yaml](../build/taskrun-helloworld.yaml) file:

Create the Task and the TaskRun:

```sh
kubectl apply -f task-helloworld.yaml
kubectl apply -f taskrun-helloworld.yaml
```

Check that both Task and TaskRun are created:

```sh
kubectl get tekton-pipelines

NAME                             SUCCEEDED   REASON      STARTTIME   COMPLETIONTIME
taskrun.tekton.dev/hello-world   True        Succeeded   7s          2s

NAME                          AGE
task.tekton.dev/hello-world   13s
```

You can see the result of TaskRun as follows:

```sh
kubectl describe taskrun echo-hello-world-task-run

...
Normal  Succeeded  119s  TaskRun  All Steps have completed executing
```

You can see that the TaskRun was successful. This confirms that Tekton is installed properly and everything is working. In the next step, we'll design and run the first build.
