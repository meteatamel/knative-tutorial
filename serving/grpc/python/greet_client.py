# Copyright 2019 Google LLC
# SPDX-License-Identifier: Apache-2.0

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
