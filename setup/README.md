# Setup

These are the steps to set Knative and its dependencies.

## Configuration

Edit [config](config) file for your setup.

## Create a GKE cluster

```sh
./create-gke-cluster
```

## Install Istio & Knative Serving

```sh
./install-serving
```

## Install Knative Eventing

```sh
./install-eventing
```

## (Optional) Create Broker

You probably need a Broker in the default namespace. You can follow instructions
in [Broker Creation](../docs/brokercreation.md) page to do that.

## (Optional) Install Knative with GCP

If you intend to read Google Cloud events, install [Knative GCP](https://github.com/google/knative-gcp) components.

There are 2 ways of setting up authentication in Knative GCP:

1. Kubernetes secrets
2. Workload identity

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
