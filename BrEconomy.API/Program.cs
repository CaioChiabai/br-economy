using BrEconomy.API.Data;
using Microsoft.EntityFrameworkCore;
using BrEconomy.API.Features.Selic.Job;
using BrEconomy.API.Features.Dolar.Job;
using BrEconomy.API.Features.IPCA.Job;
using BrEconomy.API.Features.CDI.Job;

var builder = WebApplication.CreateBuilder(args);

// --- 1. Configuração do Banco (Postgres) ---
var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");

var connectionString = string.Empty;

if (!string.IsNullOrEmpty(databaseUrl))
{
    // PRODUÇÃO (Render / Neon)
    var uri = new Uri(databaseUrl);
    var userInfo = uri.UserInfo.Split(':');

    // Porta padrão 5432 se não vier explícita
    var port = uri.Port > 0 ? uri.Port : 5432;

    connectionString =
        $"Host={uri.Host};" +
        $"Port={port};" +
        $"Database={uri.AbsolutePath.TrimStart('/')};" +
        $"Username={userInfo[0]};" +
        $"Password={userInfo[1]};" +
        "SSL Mode=Require;Trust Server Certificate=true;";
}
else
{
    // LOCAL (Docker / Dev)
    connectionString =
        builder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new Exception("ConnectionString não configurada.");
}

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

// --- 2. Configuração do Redis ---

var redisUrl = Environment.GetEnvironmentVariable("REDIS_URL");

if (!string.IsNullOrEmpty(redisUrl))
{
    var redisUri = new Uri(redisUrl);
    var redisHost = redisUri.Host;
    var redisPort = redisUri.Port > 0 ? redisUri.Port : 6379;

    var configParts = $"{redisHost}:{redisPort},abortConnect=False";

    // Adiciona senha se presente na URL (formato redis://:password@host:port)
    if (!string.IsNullOrEmpty(redisUri.UserInfo))
    {
        var password = redisUri.UserInfo.Contains(':')
            ? redisUri.UserInfo.Split(':')[1]
            : redisUri.UserInfo;
        if (!string.IsNullOrEmpty(password))
            configParts += $",password={password}";
    }

    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = configParts;
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
