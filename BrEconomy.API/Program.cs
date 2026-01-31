using BrEconomy.API.Data;
using Microsoft.EntityFrameworkCore;
using BrEconomy.API.Features.Selic.Job;
using BrEconomy.API.Features.Dolar.Job;
using BrEconomy.API.Features.IPCA.Job;
using BrEconomy.API.Features.CDI.Job;

var builder = WebApplication.CreateBuilder(args);

// --- 1. Configuração do Banco (Postgres) ---
// A string de conexão aponta para o Docker que subimos (localhost:5432)
var connectionString = "Host=localhost;Port=5432;Database=breconomy;Username=postgres;Password=postgres";
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

// --- 2. Configuração do Redis ---
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = "localhost:6379";
    options.InstanceName = "BrEconomy_";
});

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

app.Run();
