namespace Menuz.Api.Dtos;

public record LoginRequest(string Username, string Password);

public record LoginResponse(string Token, DateTime ExpiresAt, string Username, string DisplayName, string Role);

public record MeResponse(string Username, string DisplayName, string Role);
