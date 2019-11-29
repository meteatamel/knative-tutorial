# Create gRPC server & client - C#

We'll re-create the Greeter service and client mentioned in [C# Quick Start](https://grpc.io/docs/quickstart/csharp/) of gRPC docs but in ASP.NET Core.

ASP.NET Core has a new gRPC template as of [.NET Core 3.0 Preview 3](https://devblogs.microsoft.com/aspnet/asp-net-core-updates-in-net-core-3-0-preview-3/). We'll rely on that template to create our gRPC project. [gRPC for .NET](https://github.com/grpc/grpc-dotnet) has more details.

[Tutorial: Create a gRPC client and server in ASP.NET Core](https://docs.microsoft.com/en-us/aspnet/core/tutorials/grpc/grpc-start?view=aspnetcore-3.0) does a good job of explaining how to create a .NET Core gRPC client and an ASP.NET Core gRPC Server. We'll follow those steps.

Prerequisites: You have [.NET Core SDK 3.0](https://dotnet.microsoft.com/download/dotnet-core/3.0) installed.

## Create a gRPC service

First, let's make sure you have at least 3.0.X version of .NET Core:

```bash
dotnet --version

3.0.100-preview6-012264
```

Create a new gRPC project with the template:

```bash
dotnet new grpc -o GrpcGreeter

The template "ASP.NET Core gRPC Service" was created successfully.
```

Test that the gRPC service starts up locally:

```bash
cd GrpcGreeter
dotnet run

info: Microsoft.Hosting.Lifetime[0]
Now listening on: http://localhost:50051
```

## Create a gRPC client

Next, let's create a console client app to talk to the gRPC server.

```bash
dotnet new console -o GrpGreeterClient

The template "Console Application" was created successfully.
```

Add required dependencies:

```bash
cd GrpcGreeterClient
dotnet add package Grpc.Net.Client --version 0.1.22-pre2
dotnet add package Google.Protobuf
dotnet add package Grpc.Tools
```

Create `Protos` folder and add `greet.proto` from gRPC Greeter service from the previous step.  

```bash
mkdir Protos
cp ../GrpcGreeter/Protos/greet.proto Protos/
```

Edit `GrpcGreeterClient.csproj` to refer to `greet.proto`:

```xml
<ItemGroup>
  <Protobuf Include="Protos\greet.proto" GrpcServices="Client" />
</ItemGroup>
```

Now, build the project. This will generate all the gRPC classes from the service definition:

```bash
dotnet build
```

Finally, add the code to make a gRPC request. Replace [Program.cs](../serving/grpc/csharp/GrpcGreeterClient/Program.cs) contents with the following:

```csharp
using System;
using System.Net.Http;
using System.Threading.Tasks;
using GrpcGreeter;
using Grpc.Net.Client;

namespace GrpcGreeterClient
{
    class Program
    {
        static async Task Main(string[] args)
        {
            AppContext.SetSwitch(
                "System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport",
                true);
            var httpClient = new HttpClient();
            // The port number(50051) must match the port of the gRPC server.
            httpClient.BaseAddress = new Uri("http://localhost:50051");
            var client = GrpcClient.Create<Greeter.GreeterClient>(httpClient);
            var reply = await client.SayHelloAsync(
                              new HelloRequest { Name = "GreeterClient" });
            Console.WriteLine("Greeting: " + reply.Message);
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
    }
}
```

At this point, do another `dotnet build` to make sure everything still builds.

## Test gRPC client and service

Let's test everything locally now.

Inside [GrpcGreeter](../serving/grpc/csharp/GrpcGreeter) folder, start the gRPC server:

```bash
dotnet run

info: Microsoft.Hosting.Lifetime[0]
      Now listening on: http://localhost:50051
```

Inside [GrpcGreeterClient](../serving/grpc/csharp/GrpcGreeterClient) folder, start the gRPC client:

```bash
dotnet run

Greeting: Hello GreeterClient
Press any key to exit...
```

The client made a request to the server and received a response. Everything works locally!

## Create a Dockerfile

Inside [GrpcGreeter](../serving/grpc/csharp/GrpcGreeter) folder, create a [Dockerfile](../serving/grpc/csharp/GrpcGreeter/Dockerfile) for the image:

```dockerfile
FROM mcr.microsoft.com/dotnet/core/sdk:3.0

WORKDIR /app
COPY *.csproj .
RUN dotnet restore

COPY . .

RUN dotnet publish -c Release -o out

ENV PORT 8080

ENV ASPNETCORE_URLS http://*:${PORT}

CMD ["dotnet", "out/GrpcGreeter.dll"]
```

## What's Next?

[Back to Serverless gRPC with Knative](grpc.md)
