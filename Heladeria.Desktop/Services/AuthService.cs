using Heladeria.Contracts.Auth;
//using Stripe.BillingPortal;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace Heladeria.Desktop.Services
{
    public class AuthService
    {
        private readonly ApiClient _apiClient;
        private readonly SessionService _sessionService;

        public AuthService(
            ApiClient apiClient,
            SessionService sessionService)
        {
            _apiClient = apiClient;
            _sessionService = sessionService;
        }

        public async Task<LoginResponse> IniciarSesionAsync(
            LoginRequest request,
            CancellationToken cancellationToken = default)
        {
            using var response = await _apiClient.Http.PostAsJsonAsync(
                "api/auth/login",
                request,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var mensaje = await LeerMensajeErrorAsync(response);

                throw new InvalidOperationException(mensaje);
            }

            var resultado =
                await response.Content.ReadFromJsonAsync<LoginResponse>(
                    cancellationToken: cancellationToken);

            if (resultado is null ||
                string.IsNullOrWhiteSpace(resultado.AccessToken))
            {
                throw new InvalidOperationException(
                    "La API no devolvió un token válido.");
            }

            _apiClient.EstablecerToken(resultado.AccessToken);
            _sessionService.IniciarSesion(resultado);

            return resultado;
        }

        public async Task CerrarSesionAsync(
            CancellationToken cancellationToken = default)
        {
            using var response = await _apiClient.Http.PostAsync(
                "api/auth/logout",
                content: null,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var mensaje = await LeerMensajeErrorAsync(response);

                throw new InvalidOperationException(mensaje);
            }

            _apiClient.QuitarToken();
            _sessionService.CerrarSesionLocal();
        }

        private static async Task<string> LeerMensajeErrorAsync(
            HttpResponseMessage response)
        {
            var contenido =
                await response.Content.ReadAsStringAsync();

            try
            {
                using var documento = JsonDocument.Parse(contenido);
                var raiz = documento.RootElement;

                if (raiz.TryGetProperty("mensaje", out var mensaje))
                    return mensaje.GetString() ?? "La solicitud no pudo completarse.";

                if (raiz.TryGetProperty("title", out var titulo))
                    return titulo.GetString() ?? "La solicitud no pudo completarse.";

                if (raiz.TryGetProperty("errors", out var errores))
                {
                    foreach (var propiedad in errores.EnumerateObject())
                    {
                        if (propiedad.Value.ValueKind == JsonValueKind.Array &&
                            propiedad.Value.GetArrayLength() > 0)
                        {
                            return propiedad.Value[0].GetString()
                                ?? "Hay errores en los datos enviados.";
                        }
                    }
                }
            }
            catch (JsonException)
            {
                // La respuesta no contiene JSON.
            }

            return response.StatusCode switch
            {
                HttpStatusCode.Unauthorized =>
                    "Usuario o contraseña incorrectos.",

                HttpStatusCode.Forbidden =>
                    "No tienes permisos para realizar esta operación.",

                _ => "No se pudo completar la solicitud a la API."
            };
        }
    }
}
