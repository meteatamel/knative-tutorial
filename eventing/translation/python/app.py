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
from google.cloud import translate

app = Flask(__name__)


@app.route('/', methods=['POST'])
def pubsub_push():
    translation_request = get_translation_request()
    translate_text(translation_request)
    return 'OK', 200


def info(msg):
    app.logger.info(msg)


def get_translation_request():
    content = request.data.decode('utf-8')
    info(f'Translation received event: {content}')

    translation_request = json.loads(content)
    return translation_request


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


if __name__ != '__main__':
    # Redirect Flask logs to Gunicorn logs
    gunicorn_logger = logging.getLogger('gunicorn.error')
    app.logger.handlers = gunicorn_logger.handlers
    app.logger.setLevel(gunicorn_logger.level)
    info('Translation starting...')
else:
    app.run(debug=True, host='0.0.0.0', port=int(os.environ.get('PORT', 8080)))
