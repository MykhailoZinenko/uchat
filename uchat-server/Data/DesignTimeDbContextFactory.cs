using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace uchat_server.Data;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<UchatDbContext>
{
    public UchatDbContext CreateDbContext(string[] args)
    {
        DbContextOptionsBuilder<UchatDbContext> optionsBuilder = new DbContextOptionsBuilder<UchatDbContext>();
        optionsBuilder.UseSqlite("Data Source=uchat.db");

        return new UchatDbContext(optionsBuilder.Options);
    }
}
