using System.Text.RegularExpressions;
using Typewriter.CLI.Infrastructure.Models;

namespace Typewriter.CLI.Infrastructure;

/// <summary>
/// Parses .sln files to extract project entries using regex.
/// Does not require MSBuild or Visual Studio SDK.
/// </summary>
public partial class SolutionFileParser : ISolutionFileParser
{
    /// <summary>
    /// Regex pattern for parsing project lines in solution files.
    /// Captures: TypeGuid, ProjectName, RelativePath, ProjectGuid
    /// Format: Project("{TypeGuid}") = "ProjectName", "RelativePath", "{ProjectGuid}"
    /// </summary>
    [GeneratedRegex(
        """^Project\("(?<TypeGuid>\{[A-Fa-f0-9\-]+\})"\)\s*=\s*"(?<ProjectName>[^"]+)",\s*"(?<RelativePath>[^"]+)",\s*"(?<ProjectGuid>\{[A-Fa-f0-9\-]+\})"\s*$""",
        RegexOptions.Multiline)]
    private static partial Regex ProjectLineRegex();

    /// <inheritdoc/>
    public SolutionInfo? Parse(string solutionPath)
    {
        if (string.IsNullOrEmpty(solutionPath) || !File.Exists(solutionPath))
        {
            return null;
        }

        try
        {
            var fullPath = Path.GetFullPath(solutionPath);
            var content = File.ReadAllText(fullPath);
            var projects = ParseProjects(content);

            return new SolutionInfo(fullPath, projects);
        }
        catch
        {
            return null;
        }
    }

    /// <inheritdoc/>
    public IEnumerable<string> GetCSharpProjectPaths(string solutionPath)
    {
        var solutionInfo = Parse(solutionPath);
        if (solutionInfo == null)
        {
            return Enumerable.Empty<string>();
        }

        return solutionInfo.GetCSharpProjectPaths();
    }

    /// <summary>
    /// Parses project entries from solution file content.
    /// </summary>
    /// <param name="content">The content of the solution file.</param>
    /// <returns>List of parsed project entries.</returns>
    private static List<SolutionProject> ParseProjects(string content)
    {
        var projects = new List<SolutionProject>();
        var regex = ProjectLineRegex();
        var matches = regex.Matches(content);

        foreach (Match match in matches)
        {
            if (match.Success)
            {
                var typeGuidString = match.Groups["TypeGuid"].Value;
                var projectName = match.Groups["ProjectName"].Value;
                var relativePath = match.Groups["RelativePath"].Value;
                var projectGuidString = match.Groups["ProjectGuid"].Value;

                if (Guid.TryParse(typeGuidString, out var typeGuid) &&
                    Guid.TryParse(projectGuidString, out var projectGuid))
                {
                    projects.Add(new SolutionProject(
                        projectGuid,
                        projectName,
                        relativePath,
                        typeGuid));
                }
            }
        }

        return projects;
    }
}
