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
import base64
import json
import logging
import os
import pandas
import matplotlib.pyplot as plt

from flask import Flask, request
from google.cloud import bigquery
from google.cloud import storage

app = Flask(__name__)

@app.route('/', methods=['POST'])
def handle_post():
    content = read_event_data(request.data)
    country = content['country']
    tableId = content['tableId']
    query_covid_dataset(country, tableId)

    return 'OK', 200

def read_event_data(data):
    content = json.loads(request.data)

    event_data_reader = os.environ.get('EVENT_DATA_READER')

    if event_data_reader == 'PubSub':
        app.logger.info("Received CloudEvent-PubSub")
        message = content['message']
        data = message['data']
        decoded = base64.b64decode(data)
        content = json.loads(decoded)
        return content

    # TODO: Read proper CloudEvent with the SDK
    app.logger.info("Received CloudEvent-Custom")
    return content

def query_covid_dataset(country, tableId):

    app.logger.info(f"query_covid_dataset with country '{country}' and tableId '{tableId}'")

    client = bigquery.Client()

    query = f"""
        SELECT
        date, num_reports
        FROM `covid19_jhu_csse.{tableId}`
        ORDER BY date ASC"""
    app.logger.info(f'Running query: {query}')

    query_job = client.query(query)

    results = query_job.result()
    # for row in results:
    #     print("{}: {} ".format(row.date, row.num_reports))

    df = (
        results
        .to_dataframe()
    )
    app.logger.info(df.tail())

    ax = df.plot(kind='line', x='date', y='num_reports')
    ax.set_title(f'Covid Cases in {country}')
    # ax.set_xlabel('Date')
    # ax.set_ylabel('Number of cases')
    #plt.show()

    file_name = f'chart-{tableId}.png'
    app.logger.info(f'Saving file locally: {file_name}')

    plt.savefig(file_name)

    upload_blob(file_name)

def upload_blob(file_name):
    storage_client = storage.Client()
    bucket_name = os.environ.get('BUCKET')
    bucket = storage_client.bucket(bucket_name)
    blob = bucket.blob(file_name)
    blob.upload_from_filename(file_name)
    app.logger.info(f'File {file_name} uploaded to bucket {bucket_name}')


if __name__ != '__main__':
    # Redirect Flask logs to Gunicorn logs
    gunicorn_logger = logging.getLogger('gunicorn.error')
    app.logger.handlers = gunicorn_logger.handlers
    app.logger.setLevel(gunicorn_logger.level)
    plt.switch_backend('Agg') # to prevent background UI threads
    app.logger.info('Service started...')
else:
    plt.switch_backend('Agg') # to prevent background UI threads
    app.run(debug=True, host='0.0.0.0', port=int(os.environ.get('PORT', 8080)))
