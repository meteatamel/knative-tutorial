# Deploy to Cloud Run

![Serverless on Google Cloud](./images/serverless-on-google-cloud.png)

[Cloud Run](https://cloud.google.com/run/) is part of Google Cloud and it is a managed serverless platform that enables you to run stateless containers invocable via HTTP requests.  

Cloud Run is built from Knative, letting you choose to run your containers either fully managed with Cloud Run, or in your Google Kubernetes Engine cluster with Cloud Run on GKE.

The main advantage of Cloud Run is that it's fully managed, so no infrastructure to worry about. It also comes with a simple command-line and user interface to quickly deploy and manage your serverless containers.

In this lab, we will see what it takes to deploy our [Hello World Knative serving sample](01-helloworldserving.md) from the previous lab and deploy to Cloud Run. You'll be surprised how easy it is!

## Get a project in a Cloud Run region

Cloud Run is currently available in `us-central1`, `us-east1`, `europe-west1`, and `asia-northeast1` regions. Make sure your project is in one of them:

```bash
gcloud config list

[compute]
region = us-central1
zone = us-central1a
```

If not, you can set the region with `gcloud config set` command or switch to a configuration with a new project in that region:

```bash
gcloud config configurations activate cloudrun-atamel
```

You also want to make sure that the Cloud Build and Cloud Run APIs are enabled:

```bash
gcloud services enable cloudbuild.googleapis.com run.googleapis.com
```

## Push container image to Google Container Registry

Cloud Run currently deploys images from Google Container Registry (GCR) only. In [Hello World Knative serving sample](01-helloworldserving.md), we built and pushed the container image to Docker Hub. We need to push the same image to GCR.

With these changes in mind, in [helloworld](../serving/helloworld/) folder, go to the folder for the language of your choice (eg. [csharp](../serving/helloworld/csharp/)).

In [helloworld](../serving/helloworld/) folder, go to the folder for the language of your choice (eg. [csharp](../serving/helloworld/csharp/)). Run the following command. Replace `{PROJECT_ID}` with your actual Google Cloud project id:

```bash
gcloud builds submit --tag gcr.io/{PROJECT_ID}/helloworld:v1
```

This builds and pushes the image to GCR using Cloud Build.  

## Deploy to Cloud Run

Let's finally deploy our service to Cloud Run, it's a single gcloud command. Make sure you replace `{PROJECT_ID}` with your project id:

```bash
gcloud beta run deploy --image gcr.io/{PROJECT_ID}/helloworld:v1

Service name: (helloworld):
Deploying container to Cloud Run service [helloworld] in project [PROJECT_ID] region [us-central1]
Allow unauthenticated invocations to new service [helloworld]?
(y/N)?  Y

✓ Deploying new service... Done.
  ✓ Creating Revision...
  ✓ Routing traffic...
  ✓ Setting IAM Policy...
Done.
Service [helloworld] revision [helloworld-b6d89666-06c1-4de3-8955-22a1f700af8a] has been deployed and is serving traffic at https://helloworld-u6zaimzeiq-uc.a.run.app
```

This creates a Cloud Run service and a revision for the current configuration. In the end, you get a url that you can browse to.

You can also see the service in Cloud Run console:

![Cloud Run Console](./images/cloud-run-console.png)

## Test the service

We can test the service by visiting the url mentioned during deployment and in Cloud Run console. In this case, it's `https://helloworld-u6zaimyeiq-uc.a.run.app`.

One thing you might realize is that our service simply prints `Hello World` instead of `Hello v1`. Let's fix that in the last step.

## Set environment variable

If you remember, in [Hello World Knative serving sample](01-helloworldserving.md), the Knative service definition file, [service-v1.yaml](../serving/helloworld/service-v1.yaml), sets an environment variable `TARGET` and the code prints out the value of that variable:

```yaml
env:
  - name: TARGET
  value: "v1"
```

That's why our service printed `Hello v1`. We need to set the same environment variable but how do we do that in Cloud Run?

In Cloud Run, you can set environment variables either through the console or command line. Let's try the command line:

```bash
gcloud beta run services update helloworld --update-env-vars TARGET='v1'

✓ Deploying... Done.
  ✓ Creating Revision...
  ✓ Routing traffic...
Done.
```

If you visit the url of the service again, you should see `Hello v1` instead!

## What's Next?

As you've discovered, taking a Knative service and deploying to fully Cloud Run is quite easy! Next, let's take a look at Knative Eventing.

[Hello World Eventing](08-helloworldeventing.md)
