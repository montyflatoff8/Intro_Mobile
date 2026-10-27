namespace FiscApp.Models.Api;

// Client copies of FiscApi's DTOs/LoginRequest.cs and DTOs/LoginResponse.cs. The app never
// references the API project; it only has to agree on the JSON shape.

/// <summary>Body for POST api/auth/login.</summary>
public record LoginRequestDto(string Username, string Password);

/// <summary>What a successful login returns. ExpiresIn is in seconds.</summary>
public record LoginResponseDto(string AccessToken, string TokenType, int ExpiresIn);
