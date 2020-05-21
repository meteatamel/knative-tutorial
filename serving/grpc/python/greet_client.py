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

import argparse

import grpc

import greet_pb2
import greet_pb2_grpc


def run(target):
    with grpc.insecure_channel(target) as channel:
        stub = greet_pb2_grpc.GreeterStub(channel)
        response = stub.SayHello(greet_pb2.HelloRequest(name='GreeterClient'))
        print(f'Greeting: {response.message}')


def get_target(server, port):
    prefix = 'http://'
    server = server[len(prefix):] if server.startswith(prefix) else server
    return f'{server}:{port}'


if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('-s', '--server', type=str, default='localhost')
    parser.add_argument('-p', '--port', type=int, default=50051)
    args = parser.parse_args()
    run(get_target(args.server, args.port))
