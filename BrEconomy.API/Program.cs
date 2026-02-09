using BrEconomy.API.Data;
using Microsoft.EntityFrameworkCore;
using BrEconomy.API.Features.Selic.Job;
using BrEconomy.API.Features.Dolar.Job;
using BrEconomy.API.Features.IPCA.Job;
using BrEconomy.API.Features.CDI.Job;

var builder = WebApplication.CreateBuilder(args);

// --- 1. Configuração do Banco (Postgres) ---
var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");

if (!string.IsNullOrEmpty(databaseUrl))
{
    var uri = new Uri(databaseUrl);
    var userInfo = uri.UserInfo.Split(':');

    var connectionString =
        $"Host={uri.Host};" +
        $"Port={uri.Port};" +
        $"Database={uri.AbsolutePath.TrimStart('/')};" +
        $"Username={userInfo[0]};" +
        $"Password={userInfo[1]};" +
        "SSL Mode=Require;Trust Server Certificate=true;";

    builder.Configuration["ConnectionStrings:DefaultConnection"] = connectionString;
}

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection")));

// --- 2. Configuração do Redis ---

var redisUrl = Environment.GetEnvironmentVariable("REDIS_URL")
               ?? builder.Configuration["Redis:Configuration"];

if (!string.IsNullOrEmpty(redisUrl))
{
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = redisUrl;
        options.InstanceName = "BrEconomy_";
    });
}
else
{
    builder.Services.AddDistributedMemoryCache();
}

// --- 3. Configuração do HttpClient ---
builder.Services.AddHttpClient("BancoCentral", client =>
{
    client.BaseAddress = new Uri("https://api.bcb.gov.br/");
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddHostedService<SelicCurrentJob>();
builder.Services.AddHostedService<DolarCurrentJob>();
builder.Services.AddHostedService<IpcaYtdJob>();
builder.Services.AddHostedService<Ipca12MJob>();
builder.Services.AddHostedService<CdiYtdJob>();
builder.Services.AddHostedService<Cdi12MJob>();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configuração do Pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();
app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

app.Run();
