# Senzing V4 Containers

## PostgreSQL

```sh
make pg-up
make initdb
psql -h localhost -U sz -d G2
```

## SDK Tools

This container image includes the native SDK and the .NET 8 wrapper NuGet package `/opt/senzing/er/sdk/dotnet/Senzing.Sdk.4.0.0.nupkg`.

```sh
make sdktools-up
docker exec -it sz-tools bash
cd /opt/senzing/er/bin
```

## Resources

- [Senzing Docker Quickstart](https://senzing.com/docs/quickstart/quickstart_docker/)
