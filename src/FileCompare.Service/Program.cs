using FileCompare.Persistence;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder();

//builder.Services.AddDbContext<FileDbContext>(options =>
//{
//    var connectionString = new SqliteConnectionStringBuilder();
//    connectionString.DataSource = @"c:\temp\files.db";

//    options.UseSqlite(connectionString.ConnectionString);
//});

//builder.Services.AddDbContext<FileDbContext>(options => InMemoryDbContextOptionsBuilder.Default.CreateOptions(options));

builder.Services.AddDbContext<FileDbContext>(options => options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.Urls.Add("http://localhost:5000");
app.Urls.Add("http://GGAMEDESK-2:5000");
app.Urls.Add("http://192.168.178.61:5000");

app.MapGet("/", () => Results.Redirect("/swagger/index.html"));
app.MapGetFiles();
app.MapGetDifferences();
app.MapAddFiles();

var context = app.Services.CreateScope().ServiceProvider.GetRequiredService<FileDbContext>();

context.Database.EnsureCreated();

app.Run();

// for reference by integ test
public partial class Program
{ }