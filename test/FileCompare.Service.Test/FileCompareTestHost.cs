using FileCompare.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FileCompare.Service.Test;

public class FileCompareTestHostFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            var dbContext = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<FileDbContext>));

            services.Remove(dbContext!);
            services.AddDbContext<FileDbContext>(options => InMemoryDbContextOptionsBuilder.Default.CreateOptions(options));
        });
    }
}