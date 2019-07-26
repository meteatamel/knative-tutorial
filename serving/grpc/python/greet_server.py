# Copyright 2019 Google LLC
# SPDX-License-Identifier: Apache-2.0

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
