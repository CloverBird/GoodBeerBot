using GoodBeerBot.Api.Configurations;
using GoodBeerBot.Api.Extensions;
using GoodBeerBot.Api.HostesServices;
using GoodBeerBot.Api.Models;
using GoodBeerBot.Api.Services;
using Google.Apis.Sheets.v4.Data;
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

//var provider = builder.Services.BuildServiceProvider();
//var service = provider.GetRequiredService<ITableService>();
//var all = await service.GetDataItemsAsync();
//var employeeItems = all
//    .Where(i => i.Days >= 0 && i.Days <= 14 && i.Left == 0)
//    .OrderBy(i => i.Days)
//    .ToList();
//var toSurvey = employeeItems
//            .Select(i => new Position(i.Name, i.Expiry))
//            .ToList();
//await service.SavePositionsToSheetAsync(644532204, toSurvey);

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