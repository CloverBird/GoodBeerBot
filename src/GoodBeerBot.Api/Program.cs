using GoodBeerBot.Api.Configurations;
using GoodBeerBot.Api.Extensions;
using GoodBeerBot.Api.HostesServices;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

var configuration = builder.Services.LoadAppConfiguration<AppConfiguration>(builder.Configuration);

builder.Services.AddGoogleServices();
await builder.Services.AddTelegramServices(configuration.TelegramBot);

builder.Services.AddApiServices();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddHostedService<ExpiryNotificationService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapControllers();

app.Run();