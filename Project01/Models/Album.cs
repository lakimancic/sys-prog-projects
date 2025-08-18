namespace Project01.Models;

public class Album : CacheEntry
{
    public required string AlbumType { get; set; }
    public int TotalTracks { get; set; }
    public string[] AvailableMarkets { get; set; } = [];
    public required string Href { get; set; }
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required string ReleaseDate { get; set; }
    public required string ReleaseDatePrecision { get; set; }
    public required string Type { get; set; }
    public required string Uri { get; set; }
    public Artist[] Artists { get; set; } = [];
}