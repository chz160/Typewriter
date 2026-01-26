namespace SdkStyleProject.SubFolder
{
    /// <summary>
    /// A sample class in a subfolder of an SDK-style project.
    /// </summary>
    public class Class2
    {
        /// <summary>
        /// Gets or sets the description.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Calculates a result.
        /// </summary>
        /// <param name="input">The input value.</param>
        /// <returns>The calculated result.</returns>
        public int Calculate(int input) => input * 2;
    }
}