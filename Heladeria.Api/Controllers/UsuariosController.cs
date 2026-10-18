using Heladeria.Api.Data;
using Heladeria.Api.Entities;
using Heladeria.Contracts.Usuarios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Heladeria.Api.Controllers
{
    [ApiController]
    [Route("api/usuarios")]
    [Authorize(Roles = "Administrador")]
    public class UsuariosController : ControllerBase
    {
        private readonly HeladeriaDbContext _context;

        private readonly IPasswordHasher<Usuario> _passwordHasher;

        public UsuariosController(
            HeladeriaDbContext context,
            IPasswordHasher<Usuario> passwordHasher)
        {
            _context = context;
            _passwordHasher = passwordHasher;
        }

        // GET: api/usuarios
        [HttpGet]
        public async Task<IActionResult> ObtenerUsuarios(
            [FromQuery] bool incluirInactivos = false)
        {
            var consulta = _context.Usuarios
                .AsNoTracking()
                .AsQueryable();

            if (!incluirInactivos)
                consulta = consulta.Where(u => u.Activo);

            var usuarios = await consulta
                .OrderBy(u => u.NombreCompleto)
                .Select(u => new UsuarioResponse
                {
                    UsuarioId = u.UsuarioId,
                    NombreCompleto = u.NombreCompleto,
                    NombreUsuario = u.NombreUsuario,
                    Rol = u.Rol,
                    Activo = u.Activo,
                    FechaCreacion = u.FechaCreacion
                })
                .ToListAsync();

            return Ok(usuarios);
        }

        // GET: api/usuarios/2
        [HttpGet("{id:int}")]
        public async Task<IActionResult> ObtenerPorId(int id)
        {
            var usuario = await _context.Usuarios
                .AsNoTracking()
                .Where(u => u.UsuarioId == id)
                .Select(u => new UsuarioResponse
                {
                    UsuarioId = u.UsuarioId,
                    NombreCompleto = u.NombreCompleto,
                    NombreUsuario = u.NombreUsuario,
                    Rol = u.Rol,
                    Activo = u.Activo,
                    FechaCreacion = u.FechaCreacion
                })
                .FirstOrDefaultAsync();

            if (usuario is null)
            {
                return NotFound(new
                {
                    mensaje = "El usuario no existe."
                });
            }

            return Ok(usuario);
        }

        // POST: api/usuarios
        [HttpPost]
        public async Task<IActionResult> Crear(
            [FromBody] CrearUsuarioRequest request)
        {
            var nombreCompleto = request.NombreCompleto.Trim();
            var nombreUsuario = request.NombreUsuario.Trim();
            var rol = request.Rol.Trim();

            if (rol is not "Encargado" and not "Cajero")
            {
                return BadRequest(new
                {
                    mensaje = "Solo puedes crear usuarios Encargado o Cajero."
                });
            }

            if (await _context.Usuarios.AnyAsync(u =>
                u.NombreUsuario.ToUpper() == nombreUsuario.ToUpper()))
            {
                return Conflict(new
                {
                    mensaje = "Ese nombre de usuario ya está registrado."
                });
            }

            var usuario = new Usuario
            {
                NombreCompleto = nombreCompleto,
                NombreUsuario = nombreUsuario,
                Rol = rol,
                Activo = true
            };

            usuario.PasswordHash = _passwordHasher.HashPassword(
                usuario,
                request.Contrasena);

            _context.Usuarios.Add(usuario);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                // También cubre una posible duplicación simultánea.
                if (await _context.Usuarios.AnyAsync(u =>
                    u.NombreUsuario.ToUpper() == nombreUsuario.ToUpper()))
                {
                    return Conflict(new
                    {
                        mensaje = "Ese nombre de usuario ya está registrado."
                    });
                }

                throw;
            }

            return CreatedAtAction(
                nameof(ObtenerPorId),
                new { id = usuario.UsuarioId },
                ConvertirRespuesta(usuario));
        }

        // PUT: api/usuarios/2
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Actualizar(
            int id,
            [FromBody] ActualizarUsuarioRequest request)
        {
            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.UsuarioId == id);

            if (usuario is null)
            {
                return NotFound(new
                {
                    mensaje = "El usuario no existe."
                });
            }

            // La cuenta de administrador se administra por separado.
            if (usuario.Rol == "Administrador")
            {
                return BadRequest(new
                {
                    mensaje =
                        "Esta cuenta administrativa no puede modificarse " +
                        "desde la gestión de empleados."
                });
            }

            var rol = request.Rol.Trim();

            if (rol is not "Encargado" and not "Cajero")
            {
                return BadRequest(new
                {
                    mensaje = "El rol indicado no es válido."
                });
            }

            var cambioDePermisos =
                usuario.Rol != rol ||
                usuario.Activo != request.Activo;

            usuario.NombreCompleto = request.NombreCompleto.Trim();
            usuario.Rol = rol;
            usuario.Activo = request.Activo;

            // Si cambian los permisos o se desactiva la cuenta,
            // cerramos sus sesiones para invalidar sus tokens anteriores.
            if (cambioDePermisos)
            {
                var sesionesAbiertas = await _context.SesionesUsuario
                    .Where(s =>
                        s.UsuarioId == id &&
                        s.FechaHoraCierre == null)
                    .ToListAsync();

                foreach (var sesion in sesionesAbiertas)
                {
                    sesion.FechaHoraCierre = DateTime.Now;
                    sesion.MotivoCierre = "Cambio de rol o estado";
                }
            }

            await _context.SaveChangesAsync();

            return Ok(ConvertirRespuesta(usuario));
        }

        // PUT: api/usuarios/2/contrasena
        [HttpPut("{id:int}/contrasena")]
        public async Task<IActionResult> CambiarContrasena(
            int id,
            [FromBody] CambiarContrasenaUsuarioRequest request)
        {
            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.UsuarioId == id);

            if (usuario is null)
            {
                return NotFound(new
                {
                    mensaje = "El usuario no existe."
                });
            }

            if (usuario.Rol == "Administrador")
            {
                return BadRequest(new
                {
                    mensaje =
                        "La contraseña de la cuenta administrativa " +
                        "no se cambia desde la gestión de empleados."
                });
            }

            usuario.PasswordHash = _passwordHasher.HashPassword(
                usuario,
                request.NuevaContrasena);

            var sesionesAbiertas = await _context.SesionesUsuario
                .Where(s =>
                    s.UsuarioId == id &&
                    s.FechaHoraCierre == null)
                .ToListAsync();

            foreach (var sesion in sesionesAbiertas)
            {
                sesion.FechaHoraCierre = DateTime.Now;
                sesion.MotivoCierre = "Cambio de contraseña";
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                mensaje =
                    "Contraseña restablecida. El usuario deberá iniciar sesión nuevamente."
            });
        }

        private static UsuarioResponse ConvertirRespuesta(Usuario usuario)
        {
            return new UsuarioResponse
            {
                UsuarioId = usuario.UsuarioId,
                NombreCompleto = usuario.NombreCompleto,
                NombreUsuario = usuario.NombreUsuario,
                Rol = usuario.Rol,
                Activo = usuario.Activo,
                FechaCreacion = usuario.FechaCreacion
            };
        }
    }
}
