using Heladeria.Contracts.Auth;
using System;
using System.Collections.Generic;
using System.Text;

namespace Heladeria.Desktop.Services
{
    public class SessionService
    {
        public LoginResponse? UsuarioActual { get; private set; }

        public bool EstaAutenticado =>
            UsuarioActual is not null;

        public bool EsAdministrador =>
            UsuarioActual?.Rol == "Administrador";

        public bool EsEncargado =>
            UsuarioActual?.Rol == "Encargado";

        public bool EsCajero =>
            UsuarioActual?.Rol == "Cajero";

        public void IniciarSesion(LoginResponse usuario)
        {
            UsuarioActual = usuario;
        }

        public void CerrarSesionLocal()
        {
            UsuarioActual = null;
        }
    }
}
