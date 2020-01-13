# Setup

These are the steps to set Knative and its dependencies.

## Configuration

Edit [config] file for your setup.

## Create a GKE cluster

Create a GKE cluster *without* Istio add-on. We do this because the Istio version of the add-on usually lags behind what Knative expects:

```shell
./create-gke-cluster
```

## Install Istio with Cluster Local Gateway

Install Istio and also the cluster local gateway that's needed to have Knative Services as event sinks:

```shell
./install-istio
```

### Install Knative Serving

```shell
./install-serving
```

### Install Knative Eventing

```shell
./install-eventing
```

### Install Cloud Run Events

```shell
./install-cloudrun-eventing
```

-------

Thanks to [Mark Chmarny](https://github.com/mchmarny) for the idea and initial scripts.