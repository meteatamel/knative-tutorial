# Create Vision Handler - Python

## Create Vision Handler

Adapt the previous message dumper sample by creating a new [app.py](../eventing/vision/python/app.py):

```python
from flask import Flask, request
...
@app.route('/', methods=['POST'])
def storage_event():
    attr = json.loads(request.data)['Attributes']
    if attr['eventType'] == 'OBJECT_FINALIZE':
        analyze_image(attr['bucketId'], attr['objectId'])
    return 'OK', 200
```

Note: When the file is created/updated, an event of type `OBJECT_FINALIZE` is received. Bucket and file names are respectively given by `bucketId` and `objectId` attributes.

## Handle Image Analysis

Once we have the file info, we can pass it to the Vision API:

```python
from google.cloud import vision
...
def analyze_image(bucket_id, filename):
    client = vision.ImageAnnotatorClient()
    image = vision.types.Image()
    image.source.image_uri = f'gs://{bucket_id}/{filename}'
    response = client.label_detection(image=image)

    annots = response.label_annotations
    labels = ', '.join([a.description for a in annots if 0.5 <= a.score])
    info(f'Picture labels: {labels}')
```

## Create Dockerfile

Create a [Dockerfile](../eventing/vision/python/Dockerfile) for the image:

```dockerfile
FROM python:3.7-slim

RUN pip install Flask gunicorn google.cloud.vision

WORKDIR /app
COPY . .

CMD exec gunicorn --bind :$PORT --workers 1 --threads 8 app:app
```

Note: `google.cloud.vision` client library is specified in addition to `Flask` & `gunicorn`.

## What's Next?

Back to [Integrate with Vision API](visioneventing.md)
