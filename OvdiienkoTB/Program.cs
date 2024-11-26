using System.Net;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using OvdiienkoTB.Data;
using OvdiienkoTB.Models;
using OvdiienkoTB.Operations;
using OvdiienkoTB.Validation;

var builder = WebApplication.CreateBuilder(args);

var port = args.FirstOrDefault(arg => arg.StartsWith("--port="))?.Split('=')[1];
if (port != null)
{
    builder.Configuration.AddJsonFile($"Properties/{port}.json", optional: false);
}

builder.Services.Configure<NodesResponse>(builder.Configuration.GetSection("BlockchainSettings"));
var nodeUrls = builder.Configuration.GetSection("BlockchainSettings:Nodes").Get<List<string>>();
builder.Services.AddSingleton(nodeUrls);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
});

builder.Services.AddDbContext<BlockchainDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DesktopConnection")).EnableSensitiveDataLogging());

builder.Services.AddScoped<BlockchainJson>();

var nodeFilePath = builder.Configuration.GetSection("BlockchainSettings:NodeFilePath").Value;
builder.Services.AddSingleton(new JsonBlockOperations(nodeFilePath));

var mainFilePath = builder.Configuration.GetSection("BlockchainSettings:MainFilePath").Value;
builder.Services.AddSingleton(new JsonBlockOperations(mainFilePath));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var exceptionHandlerPathFeature = context.Features.Get<IExceptionHandlerPathFeature>();
        if (exceptionHandlerPathFeature?.Error is BlockchainException)
        {
            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            await context.Response.WriteAsync("Validation failed.");
        }
        else
        {
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            await context.Response.WriteAsync("An unexpected error occurred.");
        }
    });
});

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();