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
from concurrent import futures
import logging
from time import sleep

import grpc

import greet_pb2
import greet_pb2_grpc

_ONE_DAY_IN_SECONDS = 60 * 60 * 24


class Greeter(greet_pb2_grpc.GreeterServicer):
    def SayHello(self, request, context):
        logging.info(f'Request name[{request.name}]')
        return greet_pb2.HelloReply(message=f'Hello {request.name}')


def serve(port):
    logging.info(f'Starting gRPC server on port[{port}]')
    server = grpc.server(futures.ThreadPoolExecutor(max_workers=10))
    server.add_insecure_port(f'[::]:{port}')
    greet_pb2_grpc.add_GreeterServicer_to_server(Greeter(), server)

    server.start()
    try:
        while True:
            sleep(_ONE_DAY_IN_SECONDS)
    except KeyboardInterrupt:
        server.stop(0)


if __name__ == '__main__':
    logging.basicConfig(level=logging.INFO)
    parser = argparse.ArgumentParser()
    parser.add_argument('-p', '--port', type=int, default=50051)
    args = parser.parse_args()
    serve(args.port)
