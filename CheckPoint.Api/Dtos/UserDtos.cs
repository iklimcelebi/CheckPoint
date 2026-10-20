namespace CheckPoint.Api.Dtos;

/// <summary>Public profile details for a user.</summary>
public sealed record UserResponse(int Id, string Username, string Email, string Role);
