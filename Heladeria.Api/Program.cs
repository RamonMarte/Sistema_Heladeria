using Heladeria.Api.Data;
using Heladeria.Api.Entities;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "Autenticación mediante token JWT."
    });

    options.AddSecurityRequirement(document =>
        new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference("Bearer", document)] = []
        });
});

builder.Services.AddDbContext<HeladeriaDbContext>(Options =>
   Options.UseSqlServer(builder.Configuration.GetConnectionString("Heladeria")));

builder.Services.AddOpenApi();

var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException(
        "Falta configurar Jwt:Key en User Secrets.");

if (Encoding.UTF8.GetByteCount(jwtKey) < 32)
{
    throw new InvalidOperationException(
        "La clave JWT debe tener al menos 32 bytes.");
}

var jwtIssuer = builder.Configuration["Jwt:Issuer"]
    ?? "Heladeria.Api";

var jwtAudience = builder.Configuration["Jwt:Audience"]
    ?? "Heladeria.Desktop";

builder.Services.AddScoped<
    IPasswordHasher<Usuario>,
    PasswordHasher<Usuario>>();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters =
            new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = jwtIssuer,

                ValidateAudience = true,
                ValidAudience = jwtAudience,

                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(jwtKey)),

                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromSeconds(30),

                NameClaimType =
                    System.Security.Claims.ClaimTypes.Name,

                RoleClaimType =
                    System.Security.Claims.ClaimTypes.Role
            };

        // También comprobamos que la sesión siga activa en SQL Server.
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var sesionTexto = context.Principal?
                    .FindFirst("sesionId")?.Value;

                if (!Guid.TryParse(
                        sesionTexto,
                        out var sesionId))
                {
                    context.Fail("El token no contiene una sesión válida.");
                    return;
                }

                var db = context.HttpContext.RequestServices
                    .GetRequiredService<HeladeriaDbContext>();

                var sesionActiva = await db.SesionesUsuario
                    .AsNoTracking()
                    .AnyAsync(s =>
                        s.SesionId == sesionId &&
                        s.FechaHoraCierre == null &&
                        s.Usuario.Activo);

                if (!sesionActiva)
                {
                    context.Fail("La sesión ya no está activa.");
                }
            }
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider
        .GetRequiredService<HeladeriaDbContext>();

    // Solo crear el primer administrador si no hay usuarios.
    if (!await db.Usuarios.AnyAsync())
    {
        var config = scope.ServiceProvider
            .GetRequiredService<IConfiguration>();

        var nombreCompleto =
            config["BootstrapAdmin:NombreCompleto"];

        var nombreUsuario =
            config["BootstrapAdmin:NombreUsuario"];

        var contrasena =
            config["BootstrapAdmin:Contrasena"];

        if (string.IsNullOrWhiteSpace(nombreCompleto) ||
            string.IsNullOrWhiteSpace(nombreUsuario) ||
            string.IsNullOrWhiteSpace(contrasena))
        {
            throw new InvalidOperationException(
                "Configura BootstrapAdmin en User Secrets " +
                "para crear el primer administrador.");
        }

        if (contrasena.Length < 12)
        {
            throw new InvalidOperationException(
                "La contraseña inicial debe tener al menos 12 caracteres.");
        }

        var hasher = scope.ServiceProvider
            .GetRequiredService<IPasswordHasher<Usuario>>();

        var admin = new Usuario
        {
            NombreCompleto = nombreCompleto.Trim(),
            NombreUsuario = nombreUsuario.Trim(),
            Rol = "Administrador",
            Activo = true
        };

        admin.PasswordHash = hasher.HashPassword(
            admin,
            contrasena);

        db.Usuarios.Add(admin);

        await db.SaveChangesAsync();
    }
}

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
