using FileCompare.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FileCompare.Service.Test;

internal class FileCompareTestHostFactory : WebApplicationFactory<global::Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            if (services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<FileDbContext>)) is { } registered)
                services.Remove(registered);

            services.AddDbContext<FileDbContext>(options => InMemoryDbContextOptionsBuilder.Default.CreateOptions(options));
        });
    }
}