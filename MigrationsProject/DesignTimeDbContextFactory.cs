using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using DataAccess.Database;

namespace MigrationsProject
{
    public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
    {
        public ApplicationDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();

            // Строка подключения для миграций
            optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=TrainingDatabase;Username=postgres;Password=111222333",
                x => x.MigrationsAssembly("MigrationsProject")); // Указываем сборку миграций

            return new ApplicationDbContext(optionsBuilder.Options);
        }
    }
}