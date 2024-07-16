using Caches.Caches;
using Caches.Interfaces;
using MongoDbProvider;
using Repositories.Interfaces;
using Repositories.Repositories;
using Serilog;
using Services.Interfaces;
using Services.Services;

var builder = WebApplication.CreateBuilder(args);
string environmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{environmentName}.json", optional: true, reloadOnChange: true)
    .Build();
// Add services to the container.

#region Redis
//builder.Services.AddStackExchangeRedisCache(options =>
//{
//    options.Configuration = builder.Configuration.GetConnectionString("Redis");
//    options.InstanceName = "RedisTest";
//});
#endregion
Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration)
                .CreateLogger();
builder.Services.AddSingleton(Log.Logger);
builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<IStockService, StockService>();
builder.Services.AddScoped<IStockRepository, StockRepository>();
builder.Services.AddScoped<IMongoDbService, MongoDbService>();
builder.Services.AddMemoryCache();

#region CORS
var allowSpecificOrigins = "allowSpecificOrigins";
builder.Services.AddCors(options =>
{
    options.AddPolicy(name: allowSpecificOrigins,
                      policy =>
                      {
                          policy.AllowAnyMethod();
                          policy.AllowAnyMethod();
                          policy.AllowAnyOrigin();
                      });
});
#endregion
var app = builder.Build();
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors(allowSpecificOrigins);

app.UseAuthorization();

app.MapControllers();

app.Run();
