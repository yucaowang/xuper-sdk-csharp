# XuperUnion C# SDK

This is a sample C# SDK for XuperUnion.

## 1. Features

Now only support the following operations:

* Query Account Balance
* Transfer

To be added [Planned]:
* Query Tx
* Query Block
* Invoke Contract

## 2. Usage

Please referring to `Program.cs`, create a instance of `XChainSDK.XChainClient`.

The following code snippet initialize SDK client with a private key store at `./data/keys` folder and the XuperUnion node's GRPC endpoint `127.0.0.1:37101`.

```
var client = new XChainClient();
if (!client.Init("./data/keys", "127.0.0.1:37101"))
{
    Console.WriteLine("Create client failed");
    return;
}
```

Please make sure the GRPC endpoint is valid, otherwise exception would be throwed in runtime.