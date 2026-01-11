using System;
using System.Linq;
using Buildalyzer;
using Buildalyzer.Workspaces;
using Microsoft.CodeAnalysis;
using Typewriter.Configuration;
using Typewriter.Metadata.Interfaces;
using Typewriter.Metadata.Providers;

namespace Typewriter.CLI.Infrastructure
{
    public class CliMetadataProvider : IMetadataProvider
    {
        private readonly string _solutionPath;
        private AdhocWorkspace _workspace;

        public CliMetadataProvider(string solutionPath)
        {
            _solutionPath = solutionPath ?? throw new ArgumentNullException(nameof(solutionPath));
        }

        private AdhocWorkspace Workspace
        {
            get { return _workspace ?? (_workspace = LoadWorkspace()); }
        }

        private AdhocWorkspace LoadWorkspace()
        {
            var manager = new AnalyzerManager(_solutionPath);
            return manager.GetWorkspace();
        }

        public IFileMetadata GetFile(string path, Settings settings, Action<string[]> requestRender)
        {
            var documentId = Workspace.CurrentSolution.GetDocumentIdsWithFilePath(path).FirstOrDefault();
            if (documentId != null)
            {
                var document = Workspace.CurrentSolution.GetDocument(documentId);
                return new CliFileMetadata(document, settings, requestRender);
            }

            return null;
        }

        public AdhocWorkspace GetWorkspaceForValidation()
        {
            return Workspace;
        }
    }
}
