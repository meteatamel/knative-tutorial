# Copyright 2020 Google LLC
#
# Licensed under the Apache License, Version 2.0 (the "License");
# you may not use this file except in compliance with the License.
# You may obtain a copy of the License at
#
#      http://www.apache.org/licenses/LICENSE-2.0
#
# Unless required by applicable law or agreed to in writing, software
# distributed under the License is distributed on an "AS IS" BASIS,
# WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
# See the License for the specific language governing permissions and
# limitations under the License.

import json
import logging
import os

from flask import Flask, request
from google.cloud import vision

app = Flask(__name__)


@app.route('/', methods=['POST'])
def storage_event():
    content = request.data
    info(f'Vision received event: {content}')

    obj = json.loads(content)
    if obj['kind'] == 'storage#object':
        analyze_image(obj['bucket'], obj['name'])
    return 'OK', 200


def analyze_image(bucket_id, filename):
    client = vision.ImageAnnotatorClient()
    image = vision.types.Image()
    image.source.image_uri = f'gs://{bucket_id}/{filename}'
    response = client.label_detection(image=image)

    annots = response.label_annotations
    labels = ', '.join([a.description for a in annots if 0.5 <= a.score])
    info(f'Picture labels: {labels}')


def info(msg):
    app.logger.info(msg)


if __name__ != '__main__':
    # Redirect Flask logs to Gunicorn logs
    gunicorn_logger = logging.getLogger('gunicorn.error')
    app.logger.handlers = gunicorn_logger.handlers
    app.logger.setLevel(gunicorn_logger.level)
    info('Vision starting...')
else:
    app.run(debug=True, host='0.0.0.0', port=int(os.environ.get('PORT', 8080)))
