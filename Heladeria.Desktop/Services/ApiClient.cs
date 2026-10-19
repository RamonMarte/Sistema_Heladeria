using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

namespace Heladeria.Desktop.Services
{
    public class ApiClient
    {
        private readonly HttpClient _httpClient;

        public HttpClient Http => _httpClient;

        public ApiClient()
        {
            // Cambia esta URL si tu API utiliza otro puerto.
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri("https://localhost:7165/")
            };
        }

        public void EstablecerToken(string token)
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
        }

        public void QuitarToken()
        {
            _httpClient.DefaultRequestHeaders.Authorization = null;
        }
    }
}
