```
As of Knative 0.8, Knative Build has been deprecated in favor of Tekton Pipelines. This doc is kept as a reference for pre-0.8 Knative installations. Please refer to Tekton Pipelines section of the tutorial on how to do builds in Knative going forward.
```

# Kaniko Build Template

In the [previous lab](dockerbuild.md) and the lab before, we created a Build and invoked Kaniko directly, passing all the arguments required for Kaniko in the Build step. This works but a better approach is to utilize [Build Templates](https://knative.dev/docs/build/build-templates/)

Knative comes with a number of ready to use Build Templates that you can use in your Build steps. There is a [build-templates](https://github.com/knative/build-templates) repo with all the templates.

In this lab, we will use [Kaniko Build Template](https://github.com/knative/build-templates/tree/master/kaniko).

## Install Kaniko BuildTemplate

First, we need to install Kaniko Build Template:

```bash
kubectl apply -f https://raw.githubusercontent.com/knative/build-templates/master/kaniko/kaniko.yaml
```

Check that it is installed:

```bash
kubectl get buildtemplate

NAME      AGE
kaniko    24m
```

## Design the build

Let's create a Build now. Create a [buildtemplate-kaniko-helloworld-gcr.yaml](../build/deprecated/buildtemplate-kaniko-helloworld-gcr.yaml) build file:

```yaml
apiVersion: build.knative.dev/v1alpha1
kind: Build
metadata:
  name: buildtemplate-kaniko-helloworld-gcr
spec:
  source:
    git:
      url: https://github.com/meteatamel/knative-tutorial.git
      revision: master
    subPath: serving/helloworld/csharp
  template:
      name: kaniko
      arguments:
      - name: IMAGE
        # Replace {PROJECT_ID} with your GCP Project's ID.
        value: gcr.io/{PROJECT_ID}/helloworld:kaniko
```

Notice how the Build is not creating its own steps anymore but instead refers to the Kaniko template. The Docker image location is passed in via `IMAGE` argument.

## Run and watch the build

Start the build:

```bash
kubectl apply -f buildtemplate-kaniko-helloworld-gcr.yaml
```

After a few minutes, check the build is succeeded:

```bash
kubectl get build

NAME                                          SUCCEEDED
buildtemplate-kaniko-helloworld-gcr   True
```

At this point, you should see the image pushed to GCR.

## What's Next?

[Buildpacks Build Template](buildpacksbuildtemplate.md)
