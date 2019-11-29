# Create Twilio SMS handler - Python

## Create an SMS endpoint

Create a Flask app ([app.py](../serving/twilio/python/app.py)) responding a Twilio message from `GET` requests on the `/sms` endpoint:

```python
import os

from flask import Flask, request
from twilio.twiml.messaging_response import MessagingResponse

app = Flask(__name__)


@app.route('/sms', methods=['GET'])
def sms_reply():
    body = request.values.get('Body', '-')
    resp = MessagingResponse()
    resp.message(f'The Knative copy cat says: {body}')
    return str(resp)


if __name__ == '__main__':
    app.run(debug=True, host='0.0.0.0', port=int(os.environ.get('PORT', 8080)))
```

## Create a Dockerfile

Create a [Dockerfile](../serving/twilio/python/Dockerfile) for the image:

```dockerfile
FROM python:3.7-slim

RUN pip install Flask gunicorn twilio

WORKDIR /app
COPY . .

CMD exec gunicorn --bind :$PORT --workers 1 --threads 8 app:app
```

Note: The `twilio` client library is used in addition to `Flask` & `gunicorn`.

## What's Next?

[Back to Integrate with Twilio](twiliointegration.md)
