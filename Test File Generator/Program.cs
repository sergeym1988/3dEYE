using GenerateFileHandler.Application.Interfaces;
using Microsoft.AspNetCore.RateLimiting;
using System.Reflection;
using TestFileGenerator.Infrastructure.FruitProvider;
using TestFileGenerator.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCommonServices();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<IFruitProvider, FruitProvider>();
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
});
builder.Services.Configure<FileGenerationOptions>(
    builder.Configuration.GetSection("FileGeneration"));

builder.Services.AddRateLimiter(options =>
{
    options.AddSlidingWindowLimiter("sliding", limiterOptions =>
    {
        limiterOptions.Window = TimeSpan.FromSeconds(30);
        limiterOptions.PermitLimit = 10;
        limiterOptions.QueueLimit = 2;
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
