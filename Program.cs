using PrestaFlow.API.Data;
using PrestaFlow.API.Features.Auth;
using PrestaFlow.API.Features.Clientes;
using PrestaFlow.API.Features.CajaBancos;
using PrestaFlow.API.Features.Prestamos;
using PrestaFlow.API.Features.Pagos;
using PrestaFlow.API.Features.Reportes;
using PrestaFlow.API.Features.Usuarios;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ── Base de datos: Entity Framework Core + PostgreSQL ─────────────
builder.Services.AddDbContext<PrestaFlowDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// ── Acceso al contexto HTTP (para auditoría automática) ───────────
builder.Services.AddHttpContextAccessor();

// ── Servicios de la aplicación ────────────────────────────────────
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<ClientesService>();
builder.Services.AddScoped<CajaBancosService>();
builder.Services.AddScoped<PrestamosService>();
builder.Services.AddScoped<PagosService>();
builder.Services.AddScoped<ReportesService>();
builder.Services.AddScoped<UsuariosService>();

// ── Autenticación JWT ─────────────────────────────────────────────
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"]!;

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
    };
});

builder.Services.AddAuthorization();

// ── CORS: permitir peticiones desde Angular (localhost:4200) ──────
builder.Services.AddCors(options =>
{
    options.AddPolicy("PrestaFlowCors", policy =>
    {
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// ── Controladores ─────────────────────────────────────────────────
builder.Services.AddControllers();

// ── Swagger con soporte para JWT ──────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "PrestaFlow API",
        Version = "v1",
        Description = "Sistema de gestión de préstamos, cobros diarios y control de cartera de clientes."
    });

    // Agregar botón de autorización JWT en Swagger UI
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Ingresa el token JWT. Ejemplo: Bearer {tu_token}"
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

// ─────────────────────────────────────────────────────────────────
var app = builder.Build();

// ── Pipeline HTTP ─────────────────────────────────────────────────
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "PrestaFlow API v1");
    });
}

app.UseHttpsRedirection();
app.UseCors("PrestaFlowCors");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// ── Ejecutar Seeder al iniciar (crea usuario admin si no existe) ──
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<PrestaFlowDbContext>();
    await DbSeeder.SeedAsync(db);
}

app.Run();
