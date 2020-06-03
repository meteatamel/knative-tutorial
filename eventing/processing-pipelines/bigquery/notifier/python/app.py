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
from google.cloud import storage

from sendgrid import SendGridAPIClient
from sendgrid.helpers.mail import Mail

app = Flask(__name__)

@app.route('/', methods=['POST'])
def handle_post():
    # TODO: Read proper CloudEvent with the SDK
    app.logger.info(pretty_print_POST(request))
    content = json.loads(request.data)

    notify(content)
    return 'OK', 200

def notify(content):

    to_emails = os.environ.get('TO_EMAILS')
    image_url = f'https://storage.googleapis.com/{content["bucket"]}/{content["name"]}'
    app.logger.info(f"Sending email to '{to_emails}''")

    message = Mail(
        from_email='noreply@bigquery-pipeline.com',
        to_emails=to_emails,
        subject=f'A new chart from BigQuery Pipeline - {content["timeCreated"]}',
        html_content=f'A new chart is available for you to view: {image_url} <br><img src="{image_url}"/>')
    try:
        app.logger.info(f"Email content {message}")
        sg = SendGridAPIClient(os.environ.get('SENDGRID_API_KEY'))
        response = sg.send(message)
        app.logger.info(f"Email status code {response.status_code}")
    except Exception as e:
        print(e)

def pretty_print_POST(req):
    return '{}\r\n{}\r\n\r\n{}'.format(
        req.method + ' ' + req.url,
        '\r\n'.join('{}: {}'.format(k, v) for k, v in req.headers.items()),
        req.data,
    )

if __name__ != '__main__':
    # Redirect Flask logs to Gunicorn logs
    gunicorn_logger = logging.getLogger('gunicorn.error')
    app.logger.handlers = gunicorn_logger.handlers
    app.logger.setLevel(gunicorn_logger.level)
    app.logger.info('Service started...')
else:
    app.run(debug=True, host='0.0.0.0', port=int(os.environ.get('PORT', 8080)))
