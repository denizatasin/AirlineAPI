using AirlineAPI.Data;
using AirlineAPI.Middlewares;
using AirlineAPI.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Serilog;
using System.Text;

// Serilog Bootstrap Logger (Uygulama kalkana kadar geçici loglama sağlar)
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("AirlineAPI yapılandırılıyor...");

    var builder = WebApplication.CreateBuilder(args);

    // Serilog yapılandırmayı appsettings.json üzerinden okur
    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext());

    builder.Services.AddControllers();
    builder.Services.AddOpenApi();
    builder.Services.AddSwaggerGen(options =>
    {
        // Swagger'a "Bearer token ile doğrulama var" bilgisini veriyoruz
        options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Description = "Login'den dönen token'ı yapıştırın."
        });

        // Bu şemayı tüm endpoint'lere uyguluyoruz
        options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference("Bearer", document)] = []
        });
    });

    // Servis Kayıtları
    builder.Services.AddScoped<IFlightService, FlightService>();
    builder.Services.AddScoped<IAircraftService, AircraftService>();
    builder.Services.AddScoped<IFlightScheduleService, FlightScheduleService>();
    builder.Services.AddScoped<IPassengerService, PassengerService>();
    builder.Services.AddScoped<ITicketService, TicketService>();
    builder.Services.AddScoped<IDestinationService, DestinationService>();

    // DbContext Kaydı
    builder.Services.AddDbContext<AirlineDbContext>(options =>
    {
        options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
    });

    // JWT Kimlik Doğrulama
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = builder.Configuration["Jwt:Issuer"],
                ValidAudience = builder.Configuration["Jwt:Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
            };
        });

    builder.Services.AddAuthorization();

    var app = builder.Build();

    // 1. Tüm hataları yakalaması için Global Exception Middleware en üstte olmalı
    app.UseMiddleware<GlobalExceptionMiddleware>();

    // 2. HTTP istek loglaması
    app.UseSerilogRequestLogging();

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseHttpsRedirection();

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();

    Log.Information("AirlineAPI başlatılıyor...");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Uygulama beklenmeyen bir hata nedeniyle başlatılamadı!");
}
finally
{
    Log.CloseAndFlush();
}