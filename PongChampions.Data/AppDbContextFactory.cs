using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace PongChampions.Data;

public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();

        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection") ??
            $"Host=localhost;" +
            $"Port=5432;" +
            $"Database=pongchampions;" +
            $"Username={Environment.GetEnvironmentVariable("POSTGRES_USER")};" +
            $"Password={Environment.GetEnvironmentVariable("POSTGRES_PASSWORD")}";

        optionsBuilder.UseNpgsql(connectionString);

        return new AppDbContext(optionsBuilder.Options);
    }
}
