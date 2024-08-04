namespace d2.Net.App.Models;

public class Diagram
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Level { get; set; }
    public DateTime CreatedAt { get; set; }
    public string Script { get; set; } = string.Empty;
}