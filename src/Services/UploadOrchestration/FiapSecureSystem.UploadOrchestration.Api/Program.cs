using FiapSecureSystem.UploadOrchestration.Application.Abstractions;
using FiapSecureSystem.UploadOrchestration.Application.UseCases;
using FiapSecureSystem.UploadOrchestration.Infrastructure.Messaging;
using FiapSecureSystem.UploadOrchestration.Infrastructure.Persistence;
using FiapSecureSystem.UploadOrchestration.Infrastructure.Repositories;
using FiapSecureSystem.UploadOrchestration.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<UploadDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("UploadDb")));

builder.Services.AddScoped<IAnalysisRequestRepository, AnalysisRequestRepository>();
builder.Services.AddScoped<IMessageBus, FakeMessageBus>();
builder.Services.AddScoped<CreateAnalysisRequestUseCase>();

builder.Services.AddSingleton<IFileStorage>(_ =>
    new LocalFileStorage("/data/uploads"));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();

app.Run();