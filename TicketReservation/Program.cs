using System.Text.Json.Serialization;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using TicketReservation.Common.Abstractions;
using TicketReservation.Common.Behaviors;
using TicketReservation.Common.Persistence;

var builder = WebApplication.CreateBuilder(args);
var assembly = typeof(Program).Assembly;

builder.Services.AddOpenApi();
builder.Services.ConfigureHttpJsonOptions(o => o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// Yazma: EF Core, Okuma: Dapper. İkisi de aynı SQL Server veritabanına bağlanır.
builder.Services.AddDbContext<AppDbContext>(o => o.UseSqlServer(builder.Configuration.GetConnectionString("Default")));
builder.Services.AddScoped<ISqlConnectionFactory, SqlConnectionFactory>();
builder.Services.AddSingleton(TimeProvider.System);

// MediatR: tüm handler'lar bu assembly'den taranır. Behavior'lar eklenme sırasıyla çalışır (Logging -> Validation -> Handler).
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(assembly);
    cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
    cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
});
builder.Services.AddValidatorsFromAssembly(assembly);

builder.Services.AddExceptionHandler<ValidationExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddEndpoints(assembly);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    // Öğrenme projesi: geliştirmede migration'ları açılışta uygula.
    using var scope = app.Services.CreateScope();
    scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.Migrate();
}

app.UseExceptionHandler();
app.MapEndpoints();

app.Run();
