using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HEAppE.DataAccessTier
{
    internal class MiddlewareContextMigration : MiddlewareContext
    {
        #region Constructors
        public MiddlewareContextMigration() : base(true)
        {
            Database.Migrate();
        }
        #endregion
        #region Override Methods
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseLazyLoadingProxies();
            //For doing migration is needed specify connectionstring
            optionsBuilder.UseSqlServer(""); 
        }
        #endregion
    }
}
