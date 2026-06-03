namespace PriceWise.Application.Authentication.Dtos;

public sealed record RegisterRequest(string Name, string Email, string Password);
