using System.Text;
using DotNetEnv;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using PruebaTecnica_Multitenant.API.Data;
using PruebaTecnica_Multitenant.API.Services;

Env.TraversePath().Load();

var builder = WebApplication.CreateBuilder(args);

// Swagger con soporte JWT
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name        = "Authorization",
        Type        = SecuritySchemeType.Http,
        Scheme      = "Bearer",
        BearerFormat = "JWT",
        In          = ParameterLocation.Header,
        Description = "Ingresa el token: Bearer {token}"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

// PostgreSQL
var connectionString =
    $"Host={Env.GetString("DB_HOST")};" +
    $"Port={Env.GetString("DB_PORT")};" +
    $"Database={Env.GetString("DB_NAME")};" +
    $"Username={Env.GetString("DB_USER")};" +
    $"Password={Env.GetString("DB_PASSWORD")}";

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

// JWT
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer           = true,
            ValidateAudience         = true,
            ValidateLifetime         = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer              = Env.GetString("JWT_ISSUER"),
            ValidAudience            = Env.GetString("JWT_AUDIENCE"),
            IssuerSigningKey         = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(Env.GetString("JWT_SECRET")))
        };
    });

// Políticas de roles
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly",      policy => policy.RequireRole("Admin"));
    options.AddPolicy("MiembroOrAdmin", policy => policy.RequireRole("Admin", "Miembro"));
});

builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddControllers();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
