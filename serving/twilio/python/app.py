# Copyright 2019 Google LLC
# SPDX-License-Identifier: Apache-2.0

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
