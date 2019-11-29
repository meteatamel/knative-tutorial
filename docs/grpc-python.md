# Create gRPC server & client - Python

We'll re-create the Greeter service and client mentioned in [Python Quick Start](https://grpc.io/docs/quickstart/python/) of gRPC docs.

Prerequisites: You are using a recent version of Python (3.7 or earlier).

## Install gRPC

Make sure you have the latest version of the gRPC client library:

```bash
pip install --upgrade grpcio
```

## Install gRPC tools

Python’s gRPC tools include the protocol buffer compiler `protoc` and the special plugin for generating server and client code from a `.proto` service definition. Make sure you have the latest version of the gRPC tools:

```bash
pip install --upgrade grpcio-tools
```

## Create a gRPC service

Create a `protos` directory and move into:

```bash
mkdir protos
cd protos
```

Inside the `protos` directory, create a `greet.proto` service definition with a simple method/request/response:

```protobuf
syntax = "proto3";

package Greet;

service Greeter {
  rpc SayHello (HelloRequest) returns (HelloReply) {}
}

message HelloRequest {
  string name = 1;
}

message HelloReply {
  string message = 1;
}
```

Go back to your main directory and generate the interfaces:

```bash
cd ..
python -m grpc_tools.protoc greet.proto -I=protos --python_out=. --grpc_python_out=.
```

This auto-generates the `greet_pb2.py` & `greet_pb2_grpc.py` Python interfaces:

```bash
ls -l

-rw-r--r--  greet_pb2_grpc.py
-rw-r--r--  greet_pb2.py
drwxr-xr-x  protos
```

## Implement the gRPC server

Create the file [greet_server.py](../serving/grpc/python/greet_server.py):

```python
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
```

## Implement the gRPC client

Next, let's create a console client app to talk to the gRPC server.

Create the file [greet_client.py](../serving/grpc/python/greet_client.py):

```python
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
```

## Test gRPC client and service

Let's test everything locally now.

Start the gRPC server:

```bash
python greet_server.py

Starting gRPC server on port[50051]
```

In another session, launch the gRPC client:

```bash
python greet_client.py

Greeting: Hello GreeterClient
```

The client made a request to the server and received a response. Everything works locally!

## Create a Dockerfile

Create a [Dockerfile](../serving/grpc/python/Dockerfile) for the image:

```dockerfile
FROM python:3.7-slim

RUN pip install protobuf grpcio

WORKDIR /app
COPY greet_server.py greet_pb2.py greet_pb2_grpc.py ./

ENV PORT 8080
CMD python greet_server.py -p $PORT
```

## What's Next?

[Back to Serverless gRPC with Knative](grpc.md)
