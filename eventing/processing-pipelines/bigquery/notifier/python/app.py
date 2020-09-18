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
from cloudevents.http import from_http
from google.cloud import storage

from sendgrid import SendGridAPIClient
from sendgrid.helpers.mail import Mail

def read_config(var):
    value = os.environ.get(var)
    if value is None:
       raise Exception(f'{var} cannot be None')
    return value

bucket_expected = read_config('BUCKET')
to_emails = read_config('TO_EMAILS')
sendgrid_api_key = read_config('SENDGRID_API_KEY')

app = Flask(__name__)

@app.route('/', methods=['POST'])
def handle_post():
    app.logger.info(pretty_print_POST(request))

    # Read CloudEvent from the request
    cloud_event = from_http(request.headers, request.get_data())

    # Parse the event body
    bucket, name = read_event_data(cloud_event)

    # This is only needed in Cloud Run (Managed) when the
    # events are not filtered by bucket yet.
    if bucket_expected is not None and bucket != bucket_expected:
        app.logger.info(f"Input bucket '{bucket}' does not match with expected bucket '{bucket_expected}'")
    else:
        notify(bucket, name)

    return 'OK', 200

def read_event_data(cloud_event):

    # Assume custom event by default
    event_data = cloud_event.data

    type = cloud_event['type']
    # Handling new and old AuditLog types, just in case
    if type == 'google.cloud.audit.log.v1.written' or type == 'com.google.cloud.auditlog.event':
        protoPayload = event_data['protoPayload']
        resourceName = protoPayload['resourceName']
        tokens = resourceName.split('/')
        return tokens[3], tokens[5]

    return event_data["bucket"], event_data["name"]

def notify(bucket, name):

    app.logger.info(f"notify with bucket '{bucket}' and name '{name}'")

    image_url = f'https://storage.cloud.google.com/{bucket}/{name}'
    app.logger.info(f"Sending email to '{to_emails}''")

    message = Mail(
        from_email='noreply@bigquery-pipeline.com',
        to_emails=to_emails,
        subject='A new chart from BigQuery Pipeline',
        html_content=f'<html><p>A new chart is available for you to view: <a href="{image_url}">{image_url}</a></p><img src="{image_url}"></img></html>')
    try:
        app.logger.info(f"Email content {message}")
        sg = SendGridAPIClient(sendgrid_api_key)
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
