# Setup

These are the steps to set Knative and its dependencies.

## Configuration

Edit [config](config) file for your setup.

## Create a GKE cluster

Create a GKE cluster *without* Istio add-on. We do this because the Istio version of the add-on usually lags behind what Knative expects:

```sh
./create-gke-cluster
```

## Install Istio

```sh
./install-istio
```

## Install Knative Serving

```sh
./install-serving
```

## (Optional) Configure HTTPS

If you intend to use HTTPS, configure it:

```sh
./configure-https
```

## Install Knative Eventing

```sh
./install-eventing
```

## (Optional) Install observability features

Install observability features to enable logging, metrics, and request tracing in Serving and Eventing components:

```sh
./install-monitoring
```

## (Optional) Install Knative with GCP

If you intend to read Google Cloud events, install [Knative GCP](https://github.com/google/knative-gcp) components.

There are 2 ways of setting up authentication in Knative GCP:

1. Workload identity (Recommended)
2. Kubernetes secrets

Workload identity is the recommended mechanism but we have scripts for both.
Pick one of the mechanisms and use appropriate scripts.

Install Knative with GCP:

```sh
# Kubernetes secrets
./install-knative-gcp

# Workload identity
./install-knative-gcp workload
```

Configure a Pub/Sub enabled Service Account for Data Plane:

```sh
# Kubernetes secrets
./install-dataplane-serviceaccount

# Workload identity
./install-dataplane-serviceaccount workload
```

-------

Thanks to [Mark Chmarny](https://twitter.com/mchmarny) for the initial scripts
and [James Ward](https://twitter.com/_JamesWard) for HTTPS configuration
instructions.
