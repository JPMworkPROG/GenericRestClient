namespace SampleApp.Models;

public record User(
    int Id,
    string Name,
    string Email,
    string? Phone = null);

