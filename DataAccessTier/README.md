## Database operations 

### Dotnet core - Create new database migration:
```bash
# from folder /heappe-core
cd DataAcessTier 
$env:ASPNETCORE_RUNTYPE_ENVIRONMENT='LocalWindows'
dotnet ef migrations add <MigrationName> -c MiddlewareContext -o Migrations
```

### Dotnet core - Update database:
```bash
# from folder /heappe-core
cd DataAcessTier
$env:ASPNETCORE_RUNTYPE_ENVIRONMENT='LocalWindows'
dotnet ef database update -c MiddlewareContext
```

### Dotnet core - Remove database:
```bash
# from folder /heappe-core
cd DataAcessTier
$env:ASPNETCORE_RUNTYPE_ENVIRONMENT='LocalWindows'
dotnet ef migrations remove -c MiddlewareContext
```