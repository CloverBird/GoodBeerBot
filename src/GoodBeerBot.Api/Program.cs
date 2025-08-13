using GodBeerBot.Api.Configurations;
using GodBeerBot.Api.Extensions;

var builder = WebApplication.CreateBuilder(args);

var configuration = builder.Services.LoadAppConfiguration<AppConfiguration>(builder.Configuration);

builder.Services.AddApiServices();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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