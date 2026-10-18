using Heladeria.Api.Data;
using Microsoft.AspNetCore.Mvc;

//using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Heladeria.Api.Controllers
{
    [ApiController]
    [Route("api/diagnostico")]
    public class DiagnosticoController : ControllerBase
    {
        private readonly HeladeriaDbContext _context;
        private readonly ILogger<DiagnosticoController> _logger;

        public DiagnosticoController(
            HeladeriaDbContext context,
            ILogger<DiagnosticoController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet("conexion")]
        public async Task<IActionResult> VerificarConexion()
        {
            try
            {
                var cantidadUsuarios =
                    await _context.Usuarios.CountAsync();

                return Ok(new
                {
                    mensaje = "Conexión correcta con HeladeriaDB.",
                    usuariosRegistrados = cantidadUsuarios
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al comprobar la conexión con SQL Server.");

                return StatusCode(500, new
                {
                    mensaje = "No se pudo consultar la base de datos. " +
                              "Revisa la conexión y la estructura de la tabla Usuarios."
                });
            }
        }
    }
}
