## Database operations 

### Dotnet core - Create new database migration:
```bash
# from folder /heappe-core
cd DataAcessTier 
dotnet ef migrations add <MigrationName> -c MiddlewareContextMigration -o Migrations
```

### Dotnet core - Update database:
```bash
# from folder /heappe-core
cd DataAcessTier
dotnet ef database update -c MiddlewareContextMigration 
```

### Dotnet core - Remove database:
```bash
# from folder /heappe-core
cd DataAcessTier
dotnet ef migrations remove -c MiddlewareContextMigration
```
