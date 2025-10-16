# Senzing V4 .NET Lab

## Code generation

### Senzing JSON Type Definitions

`SzConnyApp/SenzingV4/Senzing.Typedef` contains generated POCO's to represent I/O for all Senzing SDK interactions.

1. Install [jtd-codegen](https://github.com/jsontypedef/json-typedef-codegen)
2. `git clone git@github.com:senzing-garage/sz-sdk-json-type-definition.git`
3. `make clean-csharp`
4. `make generate-csharp`

## Containers

### PostgreSQL

```sh
make postgres
make initdb
psql -h localhost -U sz -d G2
```

### SDK V4 Tools

The `senzingsdk-tools` container image includes the native SDK binaries, other language SDKs including the .NET 8 NuGet
package `/opt/senzing/er/sdk/dotnet/Senzing.Sdk.4.0.0.nupkg`, and various CLI utilities.

```sh
make sdktools
docker exec -it sz-tools bash
cd /opt/senzing/er/bin
```

## Resources

- [Senzing Docker Quickstart](https://senzing.com/docs/quickstart/quickstart_docker/)
