# Copyright 2019 Google LLC
# SPDX-License-Identifier: Apache-2.0

import logging
import os

from flask import Flask, request

app = Flask(__name__)


@app.route('/', methods=['POST'])
def pubsub_push():
    content = request.data.decode('utf-8')
    info(f'Event Display received event: {content}')
    return 'OK', 200

def info(msg):
    app.logger.info(msg)


if __name__ != '__main__':
    # Redirect Flask logs to Gunicorn logs
    gunicorn_logger = logging.getLogger('gunicorn.error')
    app.logger.handlers = gunicorn_logger.handlers
    app.logger.setLevel(gunicorn_logger.level)
    info('Event Display starting')
else:
    app.run(debug=True, host='0.0.0.0', port=int(os.environ.get('PORT', 8080)))
