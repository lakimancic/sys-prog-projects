namespace Project03.Models;

public class User
{
    public required string Id { get; set; }
    public required string ProfileUrl { get; set; }
    public string? ImageUrl { get; set; }
    public required string Name { get; set; }
}