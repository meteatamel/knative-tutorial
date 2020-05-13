# Setup

These are the steps to set Knative and its dependencies.

## Configuration

Edit [config](config) file for your setup.

## Create a GKE cluster

Create a GKE cluster *without* Istio add-on. We do this because the Istio version of the add-on usually lags behind what Knative expects:

```shell
./create-gke-cluster
```

## Install Istio

Install Istio:

```shell
./install-istio
```

## Install Knative Serving

```shell
./install-serving
```

## Install Knative Eventing

```shell
./install-eventing
```

## (Optional) Install observability features

Install observability features to enable logging, metrics, and request tracing in Serving and Eventing components:

```shell
./install-monitoring
```

## Install Knative with GCP

If you intend to read GCP Pub/Sub messages, go through these steps. 

There are 2 ways of setting up authentication:

1. Workload identity
2. Kubernetes secrets

To use #1, add 'workload' to the scripts.

Install Knative with GCP with Kubernetes secrets:

```bash
./install-knative-gcp
```

OR

Install Knative with GCP with Workload identity:

```bash
./install-knative-gcp workload
```

Configure a Pub/Sub enabled Service Account with Kubernetes secrets:

```bash
./install-pubsub-serviceaccount
```

OR

Configure a Pub/Sub enabled Service Account with Workload identity:

```bash
./install-pubsub-serviceaccount workload
```

-------

Thanks to [Mark Chmarny](https://github.com/mchmarny) for the idea and initial scripts.