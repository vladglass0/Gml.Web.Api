using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Gml.Domains.Integrations;
using GmlCore.Interfaces;
using Newtonsoft.Json;

namespace Gml.Web.Api.Core.Integrations.Auth;

public class UnicoreCMSAuthService(IHttpClientFactory httpClientFactory, IGmlManager gmlManager)
    : IPlatformAuthService
{
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient();

    public async Task<AuthResult> Auth(string login, string password, string? totp = null)
    {
        var authService = (await gmlManager.Integrations.GetActiveAuthService())!.Endpoint;

        var baseUri = new Uri(authService);

        var endpoint = $"{baseUri.Scheme}://{baseUri.Host}/auth/login";

        var dto = JsonConvert.SerializeObject(new
        {
            username_or_email = login,
            password,
            totp = totp ?? string.Empty,
            save_me = string.Empty
        });

        var content = new StringContent(dto, Encoding.UTF8, "application/json");

        var result =
            await _httpClient.PostAsync(endpoint, content);

        var responseResult = await result.Content.ReadAsStringAsync();

        if (responseResult.Contains("require2fa"))
        {
            return new AuthResult
            {
                IsSuccess = false,
                Message = "Введите код из приложения 2FA",
                TwoFactorEnabled = true
            };
        }

        var data = JsonConvert.DeserializeObject<UnicoreAuthResult>(responseResult);

        if (data is null || !result.IsSuccessStatusCode || data.User is null || data?.User?.Ban is not null)
        {
            if (data?.User?.Ban is { } ban)
            {
                return new AuthResult
                {
                    IsSuccess = false,
                    Message = $"Пользователь заблокирован. Причина: {ban.Reason}"
                };
            }

            return new AuthResult
            {
                IsSuccess = false,
                Message = responseResult.Contains("\"statusCode\":401")
                    ? "Неверный логин или пароль"
                    : "Произошла ошибка при обработке данных с сервера авторизации."
            };
        }

        return new ExtendedAuthResult
        {
            Login = data.User.Username ?? login,
            IsSuccess = result.IsSuccessStatusCode,
            Uuid = data.User.Uuid,
            IsSlim = data.User.Skin?.Slim ?? false,
            TwoFactorEnabled = data.User.TwoFactorEnabled is true,
            TwoFactorSecret = data.User.TwoFactorSecret?.ToString(),
            TwoFactorSecretTemp = data.User.TwoFactorSecretTemp,
            AccessToken = data.AccessToken,
            RefreshToken = data.RefreshToken
        };
    }

    /// <summary>
    /// Exchange Unicore refresh token for a new access/refresh pair.
    /// </summary>
    public async Task<ExtendedAuthResult?> RefreshAsync(string refreshToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
            return null;

        var authService = await gmlManager.Integrations.GetActiveAuthService();
        if (authService is null || string.IsNullOrWhiteSpace(authService.Endpoint))
            return null;

        var baseUri = new Uri(authService.Endpoint);
        var endpoint = $"{baseUri.Scheme}://{baseUri.Host}/auth/refresh";

        var content = new StringContent(
            JsonConvert.SerializeObject(new { refreshToken }),
            Encoding.UTF8,
            "application/json");

        var response = await _httpClient.PostAsync(endpoint, content);
        var body = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            return null;

        var data = JsonConvert.DeserializeObject<UnicoreAuthResult>(body);
        if (data is null || string.IsNullOrWhiteSpace(data.AccessToken))
            return null;

        return new ExtendedAuthResult
        {
            IsSuccess = true,
            AccessToken = data.AccessToken,
            RefreshToken = data.RefreshToken,
            Uuid = data.User?.Uuid,
            Login = data.User?.Username
        };
    }

    public static DateTime? TryGetJwtExpiry(string? jwt)
    {
        if (string.IsNullOrWhiteSpace(jwt))
            return null;

        try
        {
            var token = new JwtSecurityTokenHandler().ReadJwtToken(jwt);
            return token.ValidTo.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(token.ValidTo, DateTimeKind.Utc)
                : token.ValidTo.ToUniversalTime();
        }
        catch
        {
            return null;
        }
    }
}
