namespace LegacyProject
{
    /// <summary>
    /// Another sample class in a legacy project.
    /// </summary>
    public class Class2
    {
        /// <summary>
        /// Gets or sets the data.
        /// </summary>
        public string Data { get; set; }

        /// <summary>
        /// Processes the data.
        /// </summary>
        /// <returns>The processed data.</returns>
        public string Process()
        {
            return Data?.ToUpperInvariant() ?? string.Empty;
        }
    }
}
