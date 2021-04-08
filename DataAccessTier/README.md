## Database operations 

### Dotnet core - Create new database migration:
```bash
# from folder /heappe-core
cd DataAcessTier 
dotnet ef migrations add <MigrationName> -c MiddlewareContext
```

### Dotnet core - Update database:
```bash
# from folder /heappe-core
cd DataAcessTier
dotnet ef database update -c MiddlewareContext 
```

### Dotnet core - Remove database:
```bash
# from folder /heappe-core
cd DataAcessTier
dotnet ef migrations remove
```
