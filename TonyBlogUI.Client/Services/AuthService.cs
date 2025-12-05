using System.Net.Http.Json;
using TonyBlogUI.Shared;

namespace TonyBlogUI.Client.Services;

public class AuthService
{
    private readonly HttpClient _httpClient;
    private readonly JwtAuthenticationStateProvider _authStateProvider;

    public AuthService(HttpClient httpClient, JwtAuthenticationStateProvider authStateProvider)
    {
        _httpClient = httpClient;
        _authStateProvider = authStateProvider;
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/auth/login", request);
            var result = await response.Content.ReadFromJsonAsync<AuthResponse>();

            if (result?.Success == true && !string.IsNullOrEmpty(result.Token))
            {
                await _authStateProvider.MarkUserAsAuthenticated(result.Token);
            }

            return result ?? new AuthResponse { Success = false, Message = "Unknown error occurred" };
        }
        catch (Exception ex)
        {
            return new AuthResponse { Success = false, Message = ex.Message };
        }
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/auth/register", request);
            var result = await response.Content.ReadFromJsonAsync<AuthResponse>();

            if (result?.Success == true && !string.IsNullOrEmpty(result.Token))
            {
                await _authStateProvider.MarkUserAsAuthenticated(result.Token);
            }

            return result ?? new AuthResponse { Success = false, Message = "Unknown error occurred" };
        }
        catch (Exception ex)
        {
            return new AuthResponse { Success = false, Message = ex.Message };
        }
    }

    public async Task<AuthResponse> ChangePasswordAsync(ChangePasswordRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/auth/change-password", request);
            var result = await response.Content.ReadFromJsonAsync<AuthResponse>();

            return result ?? new AuthResponse { Success = false, Message = "Unknown error occurred" };
        }
        catch (Exception ex)
        {
            return new AuthResponse { Success = false, Message = ex.Message };
        }
    }

    public async Task<AuthResponse> GetCurrentUserAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("api/auth/me");
            
            if (!response.IsSuccessStatusCode)
            {
                return new AuthResponse { Success = false, Message = "Not authenticated" };
            }

            var result = await response.Content.ReadFromJsonAsync<AuthResponse>();
            return result ?? new AuthResponse { Success = false, Message = "Unknown error occurred" };
        }
        catch (Exception ex)
        {
            return new AuthResponse { Success = false, Message = ex.Message };
        }
    }

    public async Task LogoutAsync()
    {
        await _authStateProvider.MarkUserAsLoggedOut();
    }
}
