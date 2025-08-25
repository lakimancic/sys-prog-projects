namespace Project03.Models;

public class Review
{
    public required string Id { get; set; }
    public required string Url { get; set; }
    public required string Text { get; set; }
    public int Rating { get; set; }
    public required string TimeCreated { get; set; }
    public int MyProperty { get; set; }
    public required User User { get; set; }
    public required string[] PossibleLanguages { get; set; }
}