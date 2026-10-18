using Heladeria.Api.Data;
using Heladeria.Api.Entities;
using Heladeria.Contracts.Auth;
using LoginRequest = Heladeria.Contracts.Auth.LoginRequest;
using LoginResponse = Heladeria.Contracts.Auth.LoginResponse;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;


namespace Heladeria.Api.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly HeladeriaDbContext _context;
        private readonly IPasswordHasher<Usuario> _passwordHasher;
        private readonly IConfiguration _configuration;

        public AuthController(
            HeladeriaDbContext context,
            IPasswordHasher<Usuario> passwordHasher,
            IConfiguration configuration)
        {
            _context = context;
            _passwordHasher = passwordHasher;
            _configuration = configuration;
        }

        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> IniciarSesion(
            [FromBody] LoginRequest request)
        {
            var nombreUsuario = request.NombreUsuario.Trim();

            var usuario = await _context.Usuarios
                .SingleOrDefaultAsync(u =>
                    u.NombreUsuario == nombreUsuario);

            if (usuario is null || !usuario.Activo)
            {
                return Unauthorized(new
                {
                    mensaje = "Usuario o contraseña incorrectos."
                });
            }

            var resultado = _passwordHasher.VerifyHashedPassword(
                usuario,
                usuario.PasswordHash,
                request.Contrasena);

            if (resultado == PasswordVerificationResult.Failed)
            {
                return Unauthorized(new
                {
                    mensaje = "Usuario o contraseña incorrectos."
                });
            }

            // Actualizar el hash si el algoritmo lo recomienda.
            if (resultado ==
                PasswordVerificationResult.SuccessRehashNeeded)
            {
                usuario.PasswordHash = _passwordHasher.HashPassword(
                    usuario,
                    request.Contrasena);
            }

            var sesion = new SesionUsuario
            {
                SesionId = Guid.NewGuid(),
                UsuarioId = usuario.UsuarioId,
                Equipo = string.IsNullOrWhiteSpace(request.Equipo)
                    ? null
                    : request.Equipo.Trim(),
                DireccionIP = HttpContext.Connection.RemoteIpAddress?
                    .MapToIPv4()
                    .ToString()
            };

            _context.SesionesUsuario.Add(sesion);

            // Guardar primero la sesión para que el token
            // solo se emita cuando el registro exista.
            await _context.SaveChangesAsync();

            var issuer = _configuration["Jwt:Issuer"]
                ?? "Heladeria.Api";

            var audience = _configuration["Jwt:Audience"]
                ?? "Heladeria.Desktop";

            var key = _configuration["Jwt:Key"]
                ?? throw new InvalidOperationException(
                    "No se ha configurado la clave JWT.");

            var fechaExpiracion = DateTime.UtcNow.AddHours(8);

            var claims = new List<Claim>
        {
            new(
                ClaimTypes.NameIdentifier,
                usuario.UsuarioId.ToString()),

            new(
                ClaimTypes.Name,
                usuario.NombreUsuario),

            new(
                ClaimTypes.Role,
                usuario.Rol),

            new(
                "sesionId",
                sesion.SesionId.ToString())
        };

            var signingKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(key));

            var credentials = new SigningCredentials(
                signingKey,
                SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                notBefore: DateTime.UtcNow,
                expires: fechaExpiracion,
                signingCredentials: credentials);

            var tokenGenerado =
                new JwtSecurityTokenHandler().WriteToken(token);

            return Ok(new LoginResponse
            {
                AccessToken = tokenGenerado,
                TokenType = "Bearer",
                FechaExpiracionUtc = fechaExpiracion,
                SesionId = sesion.SesionId,
                UsuarioId = usuario.UsuarioId,
                NombreCompleto = usuario.NombreCompleto,
                NombreUsuario = usuario.NombreUsuario,
                Rol = usuario.Rol
            });
        }

        [Authorize]
        [HttpPost("logout")]
        public async Task<IActionResult> CerrarSesion()
        {
            var usuarioTexto =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            var sesionTexto =
                User.FindFirstValue("sesionId");

            if (!int.TryParse(usuarioTexto, out var usuarioId) ||
                !Guid.TryParse(sesionTexto, out var sesionId))
            {
                return Unauthorized(new
                {
                    mensaje = "No se pudo identificar la sesión."
                });
            }

            var sesion = await _context.SesionesUsuario
                .SingleOrDefaultAsync(s =>
                    s.SesionId == sesionId &&
                    s.UsuarioId == usuarioId &&
                    s.FechaHoraCierre == null);

            if (sesion is null)
            {
                return Ok(new
                {
                    mensaje = "La sesión ya estaba cerrada."
                });
            }

            sesion.FechaHoraCierre = DateTime.Now;
            sesion.MotivoCierre = "Cierre normal";

            await _context.SaveChangesAsync();

            return Ok(new
            {
                mensaje = "Sesión cerrada correctamente."
            });
        }
    }
}
