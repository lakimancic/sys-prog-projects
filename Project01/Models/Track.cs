namespace Project01.Models;

public class Track : CacheEntry
{
    public required Album Album { get; set; }
    public Artist[] Artists { get; set; } = [];
    public int DiscNumber { get; set; }
    public int DurationMs { get; set; }
    public bool Explicit { get; set; }
    public required string Href { get; set; }
    public required string Id { get; set; }
    public bool IsPlayable { get; set; }
    public required string Name { get; set; }
    public int Popularity { get; set; }
    public required string PreviewUrl { get; set; }
    public int TrackNumber { get; set; }
    public required string Type { get; set; }
    public required string Uri { get; set; }
    public bool IsLocal { get; set; }
}