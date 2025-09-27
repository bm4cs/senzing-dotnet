# Senzing V4 .NET Lab

## Containers

### PostgreSQL

```sh
make postgres
make initdb
psql -h localhost -U sz -d G2
```

### SDK V4 Tools

This container image includes the native SDK, other language SDKs including the .NET 8 NuGet package `/opt/senzing/er/sdk/dotnet/Senzing.Sdk.4.0.0.nupkg`, and various CLI utilities.

```sh
make sdktools
docker exec -it sz-tools bash
cd /opt/senzing/er/bin
```

## Resources

- [Senzing Docker Quickstart](https://senzing.com/docs/quickstart/quickstart_docker/)
