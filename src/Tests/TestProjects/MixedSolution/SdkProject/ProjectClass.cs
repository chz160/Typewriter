using MixedSolution.LegacyProject;

namespace MixedSolution.SdkProject
{
    /// <summary>
    /// A class in the SDK-style project that references the legacy project.
    /// </summary>
    public class ProjectClass
    {
        /// <summary>
        /// Gets or sets the legacy class reference.
        /// </summary>
        public LegacyClass? LegacyReference { get; set; }

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        public string? Name { get; set; }
    }
}