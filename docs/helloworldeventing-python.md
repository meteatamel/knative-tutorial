# Create Event Display - Python

## Create Event Display

Create a Flask app ([app.py](../eventing/event-display/python/app.py)) responding to `POST` requests to have a logger to print the contents of the event:

```python
import json
import logging
import os

from flask import Flask, request

app = Flask(__name__)


@app.route('/', methods=['POST'])
def pubsub_push():
    message = json.loads(request.data.decode('utf-8'))
    info(f'Event Display received message:\n{message}')
    return 'OK', 200


def info(msg):
    app.logger.info(msg)


if __name__ != '__main__':
    # Redirect Flask logs to Gunicorn logs
    gunicorn_logger = logging.getLogger('gunicorn.error')
    app.logger.handlers = gunicorn_logger.handlers
    app.logger.setLevel(gunicorn_logger.level)
else:
    app.run(debug=True, host='0.0.0.0', port=int(os.environ.get('PORT', 8080)))
```

## Create a Dockerfile

Create a [Dockerfile](../eventing/event-display/python/Dockerfile) for the image:

```dockerfile
FROM python:3.7-slim

RUN pip install Flask gunicorn

WORKDIR /app
COPY . .

CMD exec gunicorn --bind :$PORT --workers 1 --threads 8 app:app
```

## What's Next?

Back to [Hello World Eventing](helloworldeventing.md)
