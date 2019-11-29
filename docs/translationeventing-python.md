# Create Translation Handler - Python

## Create Translation Handler

Adapt the previous message dumper sample by creating a new [app.py](../eventing/translation/python/app.py):

```python
from flask import Flask, request
...

@app.route('/', methods=['POST'])
def pubsub_push():
    translation_request = get_translation_request()
    translate_text(translation_request)
    return 'OK', 200


def get_translation_request():
    message = json.loads(request.data.decode('utf-8'))
    data = base64.b64decode(message['Data'])
    translation_request = json.loads(data)
    info(f'Decoded data: {translation_request}')
    return translation_request
```

Note: The translation request is base64-encoded in the `Data` field.

## Handle Translation Request

Once we have the translation request, we can pass it to the Translation API:

```python
from google.cloud import translate
...

def translate_text(request):
    client = translate.Client()
    response = client.translate(
        request['text'],
        source_language=request['from'],
        target_language=request['to'])
    translated_text = response['translatedText']
    info(f'Translated text: {translated_text}')
    if ('detectedSourceLanguage' in response):
        detected_language = response['detectedSourceLanguage']
        info(f'Detected language: {detected_language}')
```

## Create Dockerfile

Create a [Dockerfile](../eventing/translation/python/Dockerfile) for the image:

```dockerfile
FROM python:3.7-slim

RUN pip install Flask gunicorn google.cloud.translate

WORKDIR /app
COPY . .

CMD exec gunicorn --bind :$PORT --workers 1 --threads 8 app:app
```

Note: `google.cloud.translate` client library is specified in addition to `Flask` & `gunicorn`.

## What's Next?

Back to [Integrate with Translation API](translationeventing.md)
