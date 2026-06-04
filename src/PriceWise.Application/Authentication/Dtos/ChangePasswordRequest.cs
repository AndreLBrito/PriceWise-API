namespace PriceWise.Application.Authentication.Dtos;

public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);
