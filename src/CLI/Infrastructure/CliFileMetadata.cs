using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Typewriter.Configuration;
using Typewriter.Metadata.Interfaces;
using Typewriter.Metadata.Roslyn;

namespace Typewriter.CLI.Infrastructure
{
    public class CliFileMetadata : IFileMetadata
    {
        private readonly Action<string[]> _requestRender;
        private Document _document;
        private SyntaxNode _root;
        private SemanticModel _semanticModel;

        public CliFileMetadata(Document document, Settings settings, Action<string[]> requestRender)
        {
            _requestRender = requestRender;

            LoadDocument(document);
            Settings = settings;
        }

        public Settings Settings { get; }

        public string Name
        {
            get { return _document.Name; }
        }

        public string FullName
        {
            get { return _document.FilePath; }
        }

        public IEnumerable<IClassMetadata> Classes
        {
            get { return RoslynClassMetadata.FromNamedTypeSymbols(GetNamespaceChildNodes<ClassDeclarationSyntax>(), null, Settings); }
        }

        public IEnumerable<IDelegateMetadata> Delegates
        {
            get { return RoslynDelegateMetadata.FromNamedTypeSymbols(GetNamespaceChildNodes<DelegateDeclarationSyntax>(), Settings); }
        }

        public IEnumerable<IEnumMetadata> Enums
        {
            get { return RoslynEnumMetadata.FromNamedTypeSymbols(GetNamespaceChildNodes<EnumDeclarationSyntax>(), Settings); }
        }

        public IEnumerable<IInterfaceMetadata> Interfaces
        {
            get { return RoslynInterfaceMetadata.FromNamedTypeSymbols(GetNamespaceChildNodes<InterfaceDeclarationSyntax>(), null, Settings); }
        }

        public IEnumerable<IRecordMetadata> Records
        {
            get { return RoslynRecordMetadata.FromNamedTypeSymbols(GetNamespaceChildNodes<RecordDeclarationSyntax>(), null, Settings); }
        }

        private void LoadDocument(Document document)
        {
            _document = document;
            _semanticModel = document.GetSemanticModelAsync().GetAwaiter().GetResult();
            _root = _semanticModel.SyntaxTree.GetRootAsync().GetAwaiter().GetResult();
        }

        private IEnumerable<INamedTypeSymbol> GetNamespaceChildNodes<T>()
            where T : SyntaxNode
        {
#pragma warning disable RS1039
            var symbols = _root.ChildNodes().OfType<T>().Concat(
                _root.ChildNodes().OfType<NamespaceDeclarationSyntax>().SelectMany(n => n.ChildNodes().OfType<T>())).Concat(
                    _root.ChildNodes().OfType<FileScopedNamespaceDeclarationSyntax>().SelectMany(n => n.ChildNodes().OfType<T>()))
                .Select(c => _semanticModel.GetDeclaredSymbol(c) as INamedTypeSymbol);
#pragma warning restore RS1039

            if (Settings.PartialRenderingMode == PartialRenderingMode.Combined)
            {
                return symbols.Where(s =>
                {
                    var locationToRender = s?.Locations.Select(l => l.SourceTree?.FilePath)
                        .OrderBy(f => f, StringComparer.OrdinalIgnoreCase).FirstOrDefault();
                    if (string.Equals(locationToRender, FullName, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                    else
                    {
                        if (locationToRender != null)
                        {
                            _requestRender?.Invoke(new[] { locationToRender });
                        }

                        return false;
                    }
                }).ToList();
            }

            return symbols;
        }
    }
}
