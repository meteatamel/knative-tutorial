# Copyright 2019 Google LLC
# SPDX-License-Identifier: Apache-2.0

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
