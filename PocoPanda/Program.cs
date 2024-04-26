using Cocona;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PocoPanda;
using PocoPanda.Services;
using Dapper;

[module: DapperAot(true)]

IConfiguration config = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddEnvironmentVariables()
    .Build();

var builder = CoconaApp.CreateBuilder();

builder.Services.AddScoped<RandomService>();

var app = builder.Build();

app.AddCommands<MainCommand>();

app.Run();
