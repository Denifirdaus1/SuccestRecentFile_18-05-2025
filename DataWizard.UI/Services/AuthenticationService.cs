using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace DataWizard.UI.Services
{
    public class AuthenticationService
    {
        private readonly HttpClient _client;
        private readonly string _supabaseUrl;
        private readonly string _anonKey;

        public AuthenticationService()
        {
            _supabaseUrl = "https://rrlmejrtlqnfaavyrrtf.supabase.co";
            _anonKey = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6InJybG1lanJ0bHFuZmFhdnlycnRmIiwicm9sZSI6ImFub24iLCJpYXQiOjE3NDgyMzI5NzUsImV4cCI6MjA2MzgwODk3NX0.8uC7og_bfk2C-Ok6KNGAY5Ej-nz_wBz07-94BG1rUZY";

            _client = new HttpClient();
            _client.DefaultRequestHeaders.Add("apikey", _anonKey);
            _client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_anonKey}");
        }

        public async Task<(bool success, string error)> SignInAsync(string username, string password)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Post, $"{_supabaseUrl}/auth/v1/token?grant_type=password");
                var credentials = new { email = username, password = password };
                request.Content = new StringContent(JsonSerializer.Serialize(credentials), Encoding.UTF8, "application/json");

                var response = await _client.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<dynamic>(content);
                    return (true, null);
                }

                return (false, "Invalid username or password");
            }
            catch (Exception ex)
            {
                return (false, $"Authentication error: {ex.Message}");
            }
        }

        public async Task<(bool success, string error)> SignUpAsync(string username, string password, string email, string fullName = null)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Post, $"{_supabaseUrl}/auth/v1/signup");
                var userData = new
                {
                    email = email,
                    password = password,
                    data = new
                    {
                        username = username,
                        full_name = fullName
                    }
                };

                request.Content = new StringContent(JsonSerializer.Serialize(userData), Encoding.UTF8, "application/json");

                var response = await _client.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    return (true, null);
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                return (false, $"Registration failed: {errorContent}");
            }
            catch (Exception ex)
            {
                return (false, $"Registration error: {ex.Message}");
            }
        }
    }
}