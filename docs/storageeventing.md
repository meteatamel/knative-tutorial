# Cloud Storage triggered service

In this sample, we'll take a look at how to connect Google Cloud Storage events
can be delivered to a service with Knative Eventing. We'll roughly be following
[CloudStorageSource
Example](https://github.com/google/knative-gcp/blob/master/docs/examples/cloudstoragesource/README.md)
docs page.

## Knative with GCP & PubSub Topic

We're assuming that you already went through [Install Knative with GCP](../setup/README.md) section of the setup.

## Init CloudStorageSource

Enable the Cloud Storage API:

```bash
gcloud services enable storage-component.googleapis.com storage-api.googleapis.com
```

Give Google Cloud Storage permissions to publish to GCP Pub/Sub.

First, find the Service Account that GCS uses to publish to Pub/Sub. Use the
steps outlined in [Cloud Console or the JSON
API](https://cloud.google.com/storage/docs/getting-service-account). Assuming
the service account you found from above was
`service-XYZ@gs-project-accounts.iam.gserviceaccount.com`, run the following to
grant rights to that Service Account to publish to Pub/Sub:

```bash
export GCS_SERVICE_ACCOUNT=service-XYZ@gs-project-accounts.iam.gserviceaccount.com
export PROJECT_ID=$(gcloud config get-value core/project)
gcloud projects add-iam-policy-binding $PROJECT_ID \
  --member=serviceAccount:$GCS_SERVICE_ACCOUNT \
  --role roles/pubsub.publisher
```

## Create a storage bucket

Create a unique storage bucket to save files:

```bash
export BUCKET="$(gcloud config get-value core/project)-storage"
gsutil mb gs://$BUCKET
```

## CloudStorageSource

Create a CloudStorageSource to connect storage events to Knative Eventing. The
default
[cloudstoragesource.yaml](https://github.com/google/knative-gcp/blob/master/docs/examples/cloudstoragesource/cloudstoragesource.yaml)
connects storage events to a service directly.

Instead, use the following
[cloudstoragesource.yaml](../eventing/storage/cloudstoragesource.yaml) to
connect storage events to a Broker, so, you can have multiple triggers to invoke
multiple services on the same event.

Make sure you update the bucket name.

Create the CloudStorageSource:

```bash
kubectl apply -f cloudstoragesource.yaml
```

## Broker

If there's no Broker in the default namespace already, label the namespace:

```bash
kubectl label ns default eventing.knative.dev/injection=enabled
```

You should see a Broker in the namespace:

```bash
kubectl get broker

NAME      READY   REASON   URL                                               AGE
default   True             http://default-broker.default.svc.cluster.local   52m
```

## Consumer

For the event consumer, we can use the Event Display service defined in
[kservice.yaml](../eventing/storage/kservice.yaml).

Create the service:

```bash
kubectl apply -f kservice.yaml
```

## Trigger

Connect the Event Display service to the Broker with a Trigger defined in [trigger.yaml](../eventing/storage/trigger.yaml):

Create the trigger:

```bash
kubectl apply -f trigger.yaml
```

Check that the trigger is ready:

```bash
kubectl get trigger

NAME                           READY   REASON   BROKER    SUBSCRIBER_URI                                   AGE
trigger-event-display-storage   True             default   http://event-display.default.svc.cluster.local   95s
```

## Test the service

We can now test our service by sending a file to the bucket.

```bash
gsutil cp cloudstoragesource.yaml gs://$BUCKET
```

Wait a little and check that a pod is created:

```bash
kubectl get pods
```

Inspect the logs of the pod (replace `<podid>` with actual pod id):

```bash
kubectl logs <podid> --follow -c user-container
```

You should see something similar to this:

```
info: event_display.Startup[0]
      Received CloudEvent
      ID: 1192555603721557
      Source: //storage.googleapis.com/buckets/knative-atamel-storage
      Type: com.google.cloud.storage.object.finalize
      Subject: cloudstoragesource.yaml
      DataSchema: https://raw.githubusercontent.com/google/knative-gcp/master/schemas/storage/schema.json
      DataContentType: application/json
      Time: 2020-05-13T14:52:41.114Z
      SpecVersion: V1_0
      Data: {
        "kind": "storage#object",
        "id": "knative-atamel-storage/cloudstoragesource.yaml/1589381560746021",
        "selfLink": "https://www.googleapis.com/storage/v1/b/knative-atamel-storage/o/cloudstoragesource.yaml",
        "name": "cloudstoragesource.yaml",
        "bucket": "knative-atamel-storage",
        "generation": "1589381560746021",
        "metageneration": "1",
        "contentType": "application/octet-stream",
        "timeCreated": "2020-05-13T14:52:40.745Z",
        "updated": "2020-05-13T14:52:40.745Z",
        "storageClass": "STANDARD",
        "timeStorageClassUpdated": "2020-05-13T14:52:40.745Z",
        "size": "825",
        "md5Hash": "ISwFEioV+YaifRbgswAV3w==",
        "mediaLink": "https://www.googleapis.com/download/storage/v1/b/knative-atamel-storage/o/cloudstoragesource.yaml?generation=1589381560746021&alt=media",
        "contentLanguage": "en",
        "crc32c": "41yWsw==",
        "etag": "CKWA3dqLsekCEAE="
      }
```
