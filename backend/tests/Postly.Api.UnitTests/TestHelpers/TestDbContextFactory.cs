using Microsoft.EntityFrameworkCore;
using Postly.Api.Persistence;

namespace Postly.Api.UnitTests.TestHelpers;

public static class TestDbContextFactory
{
    public static AppDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }
}
