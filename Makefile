run-server:
	cd API && dotnet watch run

create-migration:
	dotnet ef migrations add MessageEntityAdded -p API -s API -o Data/Migrations

rollback-migration:
	dotnet ef migrations remove --force -p API -s API

run-migration:
	dotnet ef database update -p API -s API

drop-db:
	dotnet ef database drop -p API -s API
