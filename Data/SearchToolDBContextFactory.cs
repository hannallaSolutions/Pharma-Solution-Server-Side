using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace SearchTool_ServerSide.Data
{
    // Used by EF Core migration tools (dotnet ef migrations add / update).
    // Bypasses Program.cs so AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true)
    // is never applied during design-time operations, keeping timestamp column types consistent
    // with the existing snapshot and previous migrations.
    public class SearchToolDBContextFactory : IDesignTimeDbContextFactory<SearchToolDBContext>
    {
        public SearchToolDBContext CreateDbContext(string[] args)
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .Build();

            var optionsBuilder = new DbContextOptionsBuilder<SearchToolDBContext>();
            optionsBuilder.UseNpgsql(configuration.GetConnectionString("SearchTool"));

            return new SearchToolDBContext(optionsBuilder.Options);
        }
    }
}
