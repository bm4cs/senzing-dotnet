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


### sz-file-loader

To ingest sample data, first register data sources with `sz_configtool`:

```sh
sz_configtool
addDataSource CUSTOMERS
addDataSource REFERENCE
addDataSource WATCHLIST
save
```

Then hydrate them with `senzing/sz-file-loader`:

1. `docker run -it --rm -u $UID -v ${PWD}/data/senzing/data/:/data --env SENZING_ENGINE_CONFIGURATION_JSON senzing/sz-file-loader -f /data/customers.jsonl`
2. `docker run -it --rm -u $UID -v ${PWD}/data/senzing/data/:/data --env SENZING_ENGINE_CONFIGURATION_JSON senzing/sz-file-loader -f /data/reference.jsonl`
3. `docker run -it --rm -u $UID -v ${PWD}/data/senzing/data/:/data --env SENZING_ENGINE_CONFIGURATION_JSON senzing/sz-file-loader -f /data/watchlist.jsonl`

Explore with `sz_explorer`.

## Resources

- [How does an Entity ID behave](https://senzing.zendesk.com/hc/en-us/articles/4415858978067-How-does-an-Entity-ID-behave)
- [code-snippets-v4](https://github.com/Senzing/code-snippets-v4)
- [Entity Specification](https://senzing.com/docs/entity_specification/)



## Resources

- [Senzing Docker Quickstart](https://senzing.com/docs/quickstart/quickstart_docker/)
- [sz-sdk-json-type-definition](https://github.com/senzing-garage/sz-sdk-json-type-definition)
- [sz_rabbit_consumer-v4](https://github.com/brianmacy/sz_rabbit_consumer-v4)
- 