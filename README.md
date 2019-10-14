# Knative Tutorial

This tutorial shows how to use different parts of [Knative](https://www.knative.dev/docs/).

## Slides

There's a [presentation](https://speakerdeck.com/meteatamel/serverless-with-knative) that accompanies the tutorial.

[![Serverless with Knative](./docs/images/serverless-with-knative.png)](https://speakerdeck.com/meteatamel/serverless-with-knative)

## Installation
If you need to install Knative and its dependencies (Istio), see [Knative Installation](https://www.knative.dev/docs/install/) page for your platform. 

For detailed GKE instructions, see [Install on Google Kubernetes Engine](https://www.knative.dev/docs/install/knative-with-gke/) page. 

We tested this tutorial on:
* Google Kubernetes Engine (GKE): 1.14.6-gke.1
* Istio: 1.1.13-gke.0
* Knative: 0.9.0

Let's briefly recap the steps of installing Knative on GKE. 

Set some environment variables for the cluster name and zone:

```bash
export CLUSTER_NAME=knative
export CLUSTER_ZONE=europe-west1-b
``` 

Create a Kubernetes cluster with Istio add-on with the preferred name and zone:

```bash
gcloud beta container clusters create $CLUSTER_NAME \
  --addons=HorizontalPodAutoscaling,HttpLoadBalancing,Istio \
  --machine-type=n1-standard-4 \
  --cluster-version=latest --zone=$CLUSTER_ZONE \
  --enable-stackdriver-kubernetes --enable-ip-alias \
  --enable-autoscaling --min-nodes=1 --max-nodes=10 \
  --enable-autorepair \
  --scopes cloud-platform
```

Grant cluster-admin permissions to the current user:

```bash
kubectl create clusterrolebinding cluster-admin-binding \
     --clusterrole=cluster-admin \
     --user=$(gcloud config get-value core/account)
```

Install Knative in 2 steps:

```bash
kubectl apply --selector knative.dev/crd-install=true \
   --filename https://github.com/knative/serving/releases/download/v0.9.0/serving.yaml \
   --filename https://github.com/knative/eventing/releases/download/v0.9.0/release.yaml \
   --filename https://github.com/knative/serving/releases/download/v0.9.0/monitoring.yaml

kubectl apply --filename https://github.com/knative/serving/releases/download/v0.9.0/serving.yaml \
   --filename https://github.com/knative/eventing/releases/download/v0.9.0/release.yaml \
   --filename https://github.com/knative/serving/releases/download/v0.9.0/monitoring.yaml
```

If everything worked, all Knative components should show a `STATUS` of `Running`:

```bash
kubectl get pods -n knative-serving
kubectl get pods -n knative-eventing
kubectl get pods -n knative-monitoring
```

## Steps

Knative Serving
* [Hello World Serving](docs/01-helloworldserving.md)
* [Configure domain](docs/02-configuredomain.md)
* [Change configuration](docs/03-changeconfig.md)
* [Traffic splitting](docs/04-trafficsplitting.md)
* [Configure autoscaling](docs/05-configureautoscaling.md)
* [Integrate with Twilio](docs/06-twiliointegration.md)
* [Deploy to Cloud Run](docs/07-deploycloudrun.md)
* [Serverless gRPC with Knative](docs/07.5-grpc.md)
* [Cluster local services](docs/07.6-clusterlocal.md)

Knative Eventing
* [Hello World Eventing](docs/08-helloworldeventing.md)
* [Integrate with Translation API](docs/09-translationeventing.md)
* [Integrate with Vision API](docs/10-visioneventing.md)

Build
* Tekton Pipelines
   * [Hello Tekton](docs/11-hellotekton.md)
   * [Hello World Build](docs/12-tekton-helloworldbuild.md)
   * [Docker Hub Build](docs/13-tekton-dockerbuild.md)
   * [Kaniko Task Build](docs/14-tekton-kanikotaskbuild.md)
* Knative Build (Deprecated) 
   * [Hello World Build](docs/deprecated/11-helloworldbuild.md)
   * [Docker Hub Build](docs/deprecated/12-dockerbuild.md)
   * [Kaniko Build Template](docs/deprecated/13-kanikobuildtemplate.md)
   * [Buildpacks Build Template](docs/deprecated/14-buildpacksbuildtemplate.md)

-------

This is not an official Google product.
