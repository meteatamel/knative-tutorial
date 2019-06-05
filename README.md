# Knative Tutorial

This tutorial shows how to use different components of [Knative](https://www.knative.dev/docs/): Build, Eventing, and Serving. 

## Slides

There's a [presentation](https://speakerdeck.com/meteatamel/serverless-with-knative) that accompanies the tutorial. Each section (Build, Eventing, Serving) has its own section in the slides.

[![Serverless with Knative](./docs/images/serverless-with-knative.png)](https://speakerdeck.com/meteatamel/serverless-with-knative)

## Pre-requisites
We assume that you have a Kubernetes cluster with Knative (and its dependency Istio) installed already. If you need to install Istio and Knative, see [Knative Installation](https://www.knative.dev/docs/install/) page. For Google Kubernetes Engine specific instructions, see [Install on Google Kubernetes Engine](https://www.knative.dev/docs/install/knative-with-gke/) page. 

We tested the tutorial on Knative version **0.6** on Google Kubernetes Engine (GKE) with Istio but the samples should work on any Kubernetes cluster with Knative.   

Before going through the tutorial, make sure all Knative components show a `STATUS` of `Running`:

```
    kubectl get pods -n knative-serving
    kubectl get pods -n knative-eventing
    kubectl get pods -n knative-build
```

## Steps

* Knative Serving
   * [Hello World Serving](docs/01-helloworldserving.md)
   * [Configure domain](docs/02-configuredomain.md)
   * [Change configuration](docs/03-changeconfig.md)
   * [Traffic splitting](docs/04-trafficsplitting.md)
   * [Configure autoscaling](docs/04.5-configureautoscaling.md)
   * [Integrate with Twilio](docs/05-twiliointegration.md)
   * [Deploy to Cloud Run](docs/05.5-deploycloudrun.md)
* Knative Eventing 
   * [Hello World Eventing](docs/06-helloworldeventing.md)
   * [Integrate with Translation API](docs/07-translationeventing.md)
   * [Integrate with Vision API](docs/08-visioneventing.md)
* Knative Build
   * [Hello World Build](docs/09-helloworldbuild.md)
   * [Docker Hub Build](docs/10-dockerbuild.md)
   * [Kaniko Build Template](docs/10.5-kanikobuildtemplate.md)
   * [Buildpacks Build Template](docs/10.7-buildpacksbuildtemplate.md)
   * [Automatic Build](docs/11-autobuild.md)
-------

This is not an official Google product.
