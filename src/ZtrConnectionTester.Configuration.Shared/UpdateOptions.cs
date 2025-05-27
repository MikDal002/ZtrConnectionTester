// In ZtrConnectionTester.Configuration.Shared/UpdateOptions.cs
namespace ZtrConnectionTester.Configuration.Shared;

public class UpdateOptions
{
    // This property name directly corresponds to the JSON key "UpdateUrl"
    public string? UpdateUrl { get; set; }
    public bool UseGitHubSource { get; set; }
    public bool FetchPrereleases { get; set; }
}
