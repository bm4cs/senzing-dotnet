up:
	docker-compose -f docker-compose-postgres.yml -f docker-compose-senzing-sdk-tools.yml up -d

down:
	docker-compose -f docker-compose-postgres.yml -f docker-compose-senzing-sdk-tools.yml down

pg-up:
	docker-compose -f docker-compose-postgres.yml up -d

pg-down:
	docker-compose -f docker-compose-postgres.yml down

postgres-delete:
	docker-compose -f docker-compose-postgres.yml down -v

initdb:
	docker-compose -f docker-compose-senzing-init-database.yml up -d

sdktools-up:
	docker-compose -f docker-compose-senzing-sdk-tools.yml up -d

sdktools-down:
	docker-compose -f docker-compose-senzing-sdk-tools.yml down

.PHONY: up down pg-up pg-down pg-delete initdb sdktools-up sdktools-down
