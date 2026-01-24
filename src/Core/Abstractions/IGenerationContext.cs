using System.Collections.Generic;

namespace Typewriter.Core.Abstractions
{
    /// <summary>
    /// Represents the context for a generation operation, providing access to
    /// the environment and services needed during template processing.
    /// </summary>
    public interface IGenerationContext
    {
        /// <summary>
        /// Gets the root path of the solution or project being processed.
        /// </summary>
        string RootPath { get; }

        /// <summary>
        /// Gets the path to the template file currently being processed.
        /// </summary>
        string TemplatePath { get; }

        /// <summary>
        /// Gets the error reporter for logging errors and warnings during generation.
        /// </summary>
        IErrorReporter ErrorReporter { get; }

        /// <summary>
        /// Gets the path resolver for resolving relative paths.
        /// </summary>
        IPathResolver PathResolver { get; }

        /// <summary>
        /// Gets any custom settings or options for the generation operation.
        /// </summary>
        IDictionary<string, object> Options { get; }
    }
}
