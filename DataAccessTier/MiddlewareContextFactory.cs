using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace HEAppE.DataAccessTier;

public class MiddlewareContextFactory : IDesignTimeDbContextFactory<MiddlewareContext>
{
    public MiddlewareContext CreateDbContext(string[] args)
    {
        return new MiddlewareContext(NullLogger<MiddlewareContext>.Instance);
    }
}
