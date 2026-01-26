namespace MixedSolution.Shared
{
    /// <summary>
    /// A shared class that lives outside the project directory.
    /// Used to test external file reference handling.
    /// </summary>
    public class SharedClass
    {
        /// <summary>
        /// Gets or sets the shared data.
        /// </summary>
        public string? SharedData { get; set; }

        /// <summary>
        /// Gets the shared constant.
        /// </summary>
        public const string SharedConstant = "SharedValue";
    }
}