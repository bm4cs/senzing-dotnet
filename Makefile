up:
	docker compose -f docker-compose-postgres.yml -f docker-compose-senzing-sdk-tools.yml up -d

down:
	docker compose -f docker-compose-postgres.yml -f docker-compose-senzing-sdk-tools.yml down

postgres:
	docker compose -f docker-compose-postgres.yml up -d

postgres-down:
	docker compose -f docker-compose-postgres.yml down

postgres-delete:
	docker compose -f docker-compose-postgres.yml down -v

initdb:
	docker compose -f docker-compose-senzing-init-database.yml up

sdktools:
	docker compose -f docker-compose-senzing-sdk-tools.yml up -d

sdktools-down:
	docker compose -f docker-compose-senzing-sdk-tools.yml down

.PHONY: up down postgres postgres-down postgres-delete initdb sdktools sdktools-down
