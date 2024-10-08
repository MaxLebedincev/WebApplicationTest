using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpClient();

builder.Services
    .AddOpenTelemetry()
    .ConfigureResource(res => res.AddService(builder.Configuration["Jaeger:ServiceName"]))
    .WithTracing(tracing => 
        tracing
        .AddSource(builder.Configuration["Jaeger:ServiceName"])
        .AddAspNetCoreInstrumentation(opt => opt.RecordException = true)
        .AddConsoleExporter()
        .AddOtlpExporter(opt => opt.Endpoint = new Uri("http://0.0.0.0:4317"))
    );

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
