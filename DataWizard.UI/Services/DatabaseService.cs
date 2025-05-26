using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Diagnostics;

namespace DataWizard.UI.Services
{
    public class DatabaseService
    {
        private readonly HttpClient _client;
        private readonly string _supabaseUrl;
        private readonly string _anonKey;

        public DatabaseService()
        {
            _supabaseUrl = "https://rrlmejrtlqnfaavyrrtf.supabase.co";
            _anonKey = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6InJybG1lanJ0bHFuZmFhdnlycnRmIiwicm9sZSI6ImFub24iLCJpYXQiOjE3NDgyMzI5NzUsImV4cCI6MjA2MzgwODk3NX0.8uC7og_bfk2C-Ok6KNGAY5Ej-nz_wBz07-94BG1rUZY";
            
            _client = new HttpClient();
            _client.DefaultRequestHeaders.Add("apikey", _anonKey);
            _client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_anonKey}");
        }

        private async Task<T> SendRequestAsync<T>(string endpoint, HttpMethod method, object data = null)
        {
            try
            {
                var request = new HttpRequestMessage(method, $"{_supabaseUrl}/rest/v1/{endpoint}");
                
                if (data != null)
                {
                    var json = JsonSerializer.Serialize(data);
                    request.Content = new StringContent(json, Encoding.UTF8, "application/json");
                }

                request.Headers.Add("Prefer", "return=representation");

                var response = await _client.SendAsync(request);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<T>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"API Error: {ex.Message}");
                throw;
            }
        }

        public async Task<(bool success, string error)> ValidateUserCredentialsAsync(string username, string password)
        {
            try
            {
                var users = await SendRequestAsync<List<dynamic>>(
                    $"users?username=eq.{username}&password=eq.{password}&select=id,username,email,full_name",
                    HttpMethod.Get);

                if (users != null && users.Count > 0)
                {
                    await SendRequestAsync<dynamic>(
                        $"users?id=eq.{users[0].id}",
                        HttpMethod.Patch,
                        new { last_login_at = DateTime.UtcNow });

                    return (true, null);
                }

                return (false, "Invalid username or password");
            }
            catch (Exception ex)
            {
                return (false, $"Database error: {ex.Message}");
            }
        }

        public async Task<(bool success, string error)> CreateUserAsync(string username, string password, string email, string fullName)
        {
            try
            {
                var newUser = new
                {
                    username = username,
                    password = password,
                    email = email,
                    full_name = fullName ?? string.Empty,
                    created_at = DateTime.UtcNow
                };

                await SendRequestAsync<dynamic>("users", HttpMethod.Post, newUser);
                return (true, null);
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("duplicate"))
                {
                    return (false, "Username or email already exists");
                }
                return (false, $"Database error: {ex.Message}");
            }
        }

        public async Task<List<OutputFile>> GetRecentFilesAsync(int userId, int count = 4)
        {
            try
            {
                var files = await SendRequestAsync<List<OutputFile>>(
                    $"output_files?user_id=eq.{userId}&order=created_at.desc&limit={count}",
                    HttpMethod.Get);
                return files ?? new List<OutputFile>();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error fetching recent files: {ex.Message}");
                return new List<OutputFile>();
            }
        }

        public async Task<List<Folder>> GetUserFoldersAsync(int userId, int count = 4)
        {
            try
            {
                var folders = await SendRequestAsync<List<Folder>>(
                    $"folders?user_id=eq.{userId}&order=updated_at.desc&limit={count}",
                    HttpMethod.Get);
                return folders ?? new List<Folder>();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error fetching folders: {ex.Message}");
                return new List<Folder>();
            }
        }

        public async Task<List<ChartData>> GetFileTypeStatsAsync(int userId)
        {
            try
            {
                var stats = await SendRequestAsync<List<ChartData>>(
                    $"rpc/get_input_file_type_stats?p_user_id={userId}",
                    HttpMethod.Post);
                return stats ?? new List<ChartData>();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error fetching file type stats: {ex.Message}");
                return new List<ChartData>();
            }
        }

        public async Task<string> GetUserPreferredFormatAsync(int userId)
        {
            try
            {
                var preferences = await SendRequestAsync<List<dynamic>>(
                    $"user_preferences?user_id=eq.{userId}&select=format",
                    HttpMethod.Get);
                
                if (preferences != null && preferences.Count > 0)
                {
                    return preferences[0].format;
                }
                return "Excel";
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting user format preference: {ex.Message}");
                return "Excel";
            }
        }

        public async Task SaveUserPreferredFormatAsync(int userId, string format)
        {
            try
            {
                var preference = new { user_id = userId, format = format };
                await SendRequestAsync<dynamic>("user_preferences", HttpMethod.Post, preference);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error saving user format preference: {ex.Message}");
            }
        }

        public async Task<int> LogHistoryAsync(int userId, int inputFileTypeId, int outputFormatId, string prompt, string processType)
        {
            try
            {
                var history = new
                {
                    user_id = userId,
                    input_file_type_id = inputFileTypeId,
                    output_format_id = outputFormatId,
                    prompt_text = prompt,
                    process_type = processType
                };

                var result = await SendRequestAsync<dynamic>("history", HttpMethod.Post, history);
                return result?.id ?? -1;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error logging history: {ex.Message}");
                return -1;
            }
        }

        public async Task UpdateHistoryProcessingTimeAsync(int historyId, int processingTimeMs)
        {
            try
            {
                await SendRequestAsync<dynamic>(
                    $"history?id=eq.{historyId}",
                    HttpMethod.Patch,
                    new { processing_time = processingTimeMs });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error updating history processing time: {ex.Message}");
            }
        }

        public async Task UpdateHistoryStatusAsync(int historyId, bool isSuccess, int processingTimeMs)
        {
            try
            {
                await SendRequestAsync<dynamic>(
                    $"history?id=eq.{historyId}",
                    HttpMethod.Patch,
                    new { is_success = isSuccess, processing_time = processingTimeMs });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error updating history status: {ex.Message}");
            }
        }

        public async Task LogOutputFileAsync(int historyId, string fileName, string filePath, long fileSize)
        {
            try
            {
                var outputFile = new
                {
                    history_id = historyId,
                    name = fileName,
                    path = filePath,
                    size = fileSize
                };

                await SendRequestAsync<dynamic>("output_files", HttpMethod.Post, outputFile);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error logging output file: {ex.Message}");
            }
        }

        public async Task<int> GetFileTypeId(string typeName)
        {
            try
            {
                var fileTypes = await SendRequestAsync<List<dynamic>>(
                    $"file_types?name=eq.{typeName}&select=id",
                    HttpMethod.Get);

                if (fileTypes != null && fileTypes.Count > 0)
                {
                    return fileTypes[0].id;
                }

                // Default to 'OTHER' if type not found
                return await GetFileTypeId("OTHER");
            }
            catch
            {
                return await GetFileTypeId("OTHER");
            }
        }

        public async Task<int> GetOutputFormatId(string formatName)
        {
            try
            {
                var formats = await SendRequestAsync<List<dynamic>>(
                    $"output_formats?name=eq.{formatName}&select=id",
                    HttpMethod.Get);

                if (formats != null && formats.Count > 0)
                {
                    return formats[0].id;
                }
                return 1; // Default to Excel (ID=1)
            }
            catch
            {
                return 1;
            }
        }

        public async Task<List<HistoryItem>> GetRecentHistoryAsync(int userId, int count)
        {
            try
            {
                var query = $@"history?user_id=eq.{userId}
                    &select=id,input_file_type_id(name),output_format_id(name),
                    process_date,processing_time,is_success,process_type
                    &order=process_date.desc
                    &limit={count}";

                var history = await SendRequestAsync<List<dynamic>>(query, HttpMethod.Get);
                var historyList = new List<HistoryItem>();

                foreach (var item in history)
                {
                    historyList.Add(new HistoryItem
                    {
                        HistoryId = item.id,
                        InputType = item.input_file_type_id.name,
                        OutputFormat = item.output_format_id.name,
                        ProcessDate = item.process_date,
                        ProcessingTime = item.processing_time,
                        IsSuccess = item.is_success,
                        ProcessType = item.process_type
                    });
                }

                return historyList;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in GetRecentHistoryAsync: {ex}");
                throw;
            }
        }
    }

    // Model classes remain unchanged
    public class OutputFile
    {
        public int FileId { get; set; }
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public long FileSize { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    public class HistoryItem
    {
        public int HistoryId { get; set; }
        public string InputType { get; set; }
        public string OutputFormat { get; set; }
        public DateTime ProcessDate { get; set; }
        public int ProcessingTime { get; set; }
        public bool IsSuccess { get; set; }
        public string ProcessType { get; set; }
    }

    public class Folder
    {
        public int FolderId { get; set; }
        public string FolderName { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    public class ChartData
    {
        public string Label { get; set; }
        public int Value { get; set; }
    }

    public class SavedFile
    {
        public Guid Id { get; set; }
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}