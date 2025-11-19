namespace SampleApp.Models;

public record Post(
    int Id,
    int UserId,
    string Title,
    string Body);

public record CreatePostRequest(
    int UserId,
    string Title,
    string Body);

public record UpdatePostRequest(
    string Title,
    string Body);

