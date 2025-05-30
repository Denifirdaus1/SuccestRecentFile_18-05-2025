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
                var request = new HttpRequestMessage(HttpMethod.Get, $"{_supabaseUrl}/rest/v1/users?username=eq.{username}&select=*");
                request.Headers.Add("Prefer", "return=representation");

                var response = await _client.SendAsync(request);
                var content = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    return (false, $"Login failed: {response.StatusCode}");
                }

                var users = JsonSerializer.Deserialize<JsonElement[]>(content);
                if (users == null || users.Length == 0)
                {
                    return (false, "User not found");
                }

                var user = users[0];
                if (user.GetProperty("password").GetString() == password)
                {
                    // Update last login
                    var updateRequest = new HttpRequestMessage(HttpMethod.Patch, 
                        $"{_supabaseUrl}/rest/v1/users?id=eq.{user.GetProperty("id").GetString()}");
                    
                    var updateData = JsonSerializer.Serialize(new { last_login_at = DateTime.UtcNow });
                    updateRequest.Content = new StringContent(updateData, Encoding.UTF8, "application/json");
                    updateRequest.Headers.Add("Prefer", "return=minimal");
                    
                    await _client.SendAsync(updateRequest);
                    return (true, null);
                }

                return (false, "Invalid password");
            }
            catch (Exception ex)
            {
                return (false, $"Authentication error: {ex.Message}");
            }
        }

        public async Task<(bool success, string error)> SignUpAsync(string username, string password, string email)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Post, $"{_supabaseUrl}/rest/v1/users");
                
                var userData = new
                {
                    id = Guid.NewGuid(),
                    username = username,
                    password = password,
                    email = email,
                    created_at = DateTime.UtcNow,
                    is_active = true
                };

                var json = JsonSerializer.Serialize(userData);
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");
                request.Headers.Add("Prefer", "return=minimal");

                var response = await _client.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    return (true, null);
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                if (errorContent.Contains("duplicate"))
                {
                    return (false, "Username or email already exists");
                }

                return (false, $"Registration failed: {errorContent}");
            }
            catch (Exception ex)
            {
                return (false, $"Registration error: {ex.Message}");
            }
        }
    }
}