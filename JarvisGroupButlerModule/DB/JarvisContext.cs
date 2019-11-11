using SQLite.CodeFirst;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JarvisGroupButlerModule.DB
{
    internal class JarvisContext : DbContext
    {
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            var sqliteConnectionInitializer = new SqliteCreateDatabaseIfNotExists<JarvisContext>(modelBuilder);
            Database.SetInitializer(sqliteConnectionInitializer);
        }

        public JarvisContext(string filename) : base($"Data Source={filename}")
        {

        }

        public DbSet<DbUser> Users { get; set; }
    }
}
