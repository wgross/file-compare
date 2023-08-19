using FileCompare.Persistence;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder();

builder.Services.AddDbContext<FileDbContext>(options => options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

if (!builder.Environment.IsDevelopment())
{
    // attach to linux systemd lifecycle management
    builder.Host.UseSystemd();
};

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.Urls.Add("http://0.0.0.0:5000");

app.MapGet("/", () => Results.Redirect("/swagger/index.html"));
app.MapGetFiles();
app.MapGetDifferences();
app.MapGetDuplicates();
app.MapAddFiles();

var context = app.Services
    .CreateScope().ServiceProvider
    .GetRequiredService<FileDbContext>()
    .Database.EnsureCreated();

app.Run();