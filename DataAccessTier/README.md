## Database operations 

### Dotnet core - Create new database migration:
```bash
# from folder /heappe-core
cd DataAccessTier 
$env:ASPNETCORE_RUNTYPE_ENVIRONMENT='LocalWindows'
dotnet ef migrations add <MigrationName> -c MiddlewareContext -o Migrations
```

### Dotnet core - Update database:
```bash
# from folder /heappe-core
cd DataAccessTier
$env:ASPNETCORE_RUNTYPE_ENVIRONMENT='LocalWindows'
dotnet ef database update -c MiddlewareContext
```

### Dotnet core - Remove database:
```bash
# from folder /heappe-core
cd DataAccessTier
$env:ASPNETCORE_RUNTYPE_ENVIRONMENT='LocalWindows'
dotnet ef migrations remove -c MiddlewareContext
```

## Troubleshooting steps
- Use the Package Manager Console in the Visual Studio (View > Other Windows > Package Manager Console) and make sure to set the Default project to the **DataAccessTier**.
- To debug failing command you can use the `--verbose` switch.


If you get the following error, make sure to check these requirements:
```
Microsoft.EntityFrameworkCore.Design.OperationException: Unable to create an object of type 'MiddlewareContext'. For the different patterns supported at design time, see https://go.microsoft.com/fwlink/?linkid=851728
```
- The Default project of the Package Manager Console is se to the **DataAccessTier**
- The connection string in the `DataAccessTier.OnConfiguring()` is set to the correct one, e.g.: 
```cs
protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
{
    optionsBuilder.UseLazyLoadingProxies();
    // Connection to the SQL server running in the docker container via port 6000
    optionsBuilder.UseSqlServer("Server=localhost,6000;Database=heappe;User=SA;Password=<password>;");
}
```
- If the migration or update fails with this exception:
```
System.ApplicationException: Application and database migrations are not the same. Please update the database to the new version.
```
- you need to set the Environmental variable `ASPNETCORE_RUNTYPE_ENVIRONMENT`, this can be done in the Package Manager Console with the following command:
```ps
$env:ASPNETCORE_RUNTYPE_ENVIRONMENT = "LocalWindows"
```
