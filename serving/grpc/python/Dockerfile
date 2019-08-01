# Use an official lightweight Python image.
# https://hub.docker.com/_/python
FROM python:3.7-slim

# Install production dependencies.
RUN pip install protobuf grpcio

# Copy local code to the container image.
WORKDIR /app
COPY greet_server.py greet_pb2.py greet_pb2_grpc.py ./

ENV PORT 8080
CMD python greet_server.py -p $PORT
