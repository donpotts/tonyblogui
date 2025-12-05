namespace TonyBlogUI.Shared;

public class Blog : IEntityWithId
{
    public string Id { get; set; } = string.Empty;
    public string ClusterName { get; set; } = string.Empty;
    public string Intent { get; set; } = string.Empty;
    public List<string> Keywords { get; set; } = new();
    public string PrimaryKeyword { get; set; } = string.Empty;
    public bool Completed { get; set; }
    public string Url { get; set; } = string.Empty;
}
