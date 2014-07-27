using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Text;
using Microsoft.Framework.Runtime;

namespace Kake
{
    /// <summary>
    /// Summary description for RoslynCompilerService
    /// </summary>
    public class RoslynCompilationService
    {
        private static readonly ConcurrentDictionary<string, MetadataReference> _metadataFileCache =
            new ConcurrentDictionary<string, MetadataReference>(StringComparer.OrdinalIgnoreCase);

        private readonly ILibraryManager _libraryManager;
        private readonly IApplicationEnvironment _environment;
        private readonly IAssemblyLoaderEngine _loader;

        public RoslynCompilationService(IApplicationEnvironment environment,
                                        IAssemblyLoaderEngine loaderEngine,
                                        ILibraryManager libraryManager)
        {
            _environment = environment;
            _loader = loaderEngine;
            _libraryManager = libraryManager;
        }

        public Assembly Compile(CSharpSyntaxTree syntaxTree, IEnumerable<string> referenceNames, string assemblyName)
        {
            var syntaxTrees = new[] { syntaxTree };
            var targetFramework = _environment.TargetFramework;

            var references = GetReferences(referenceNames);

            var compilation = CSharpCompilation.Create(assemblyName,
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary),
                syntaxTrees: syntaxTrees,
                references: references);

            using (var ms = new MemoryStream())
            {
                using (var pdb = new MemoryStream())
                {
                    EmitResult result;

                    if (PlatformHelper.IsMono)
                    {
                        result = compilation.Emit(ms, pdbStream: null);
                    }
                    else
                    {
                        result = compilation.Emit(ms, pdbStream: pdb);
                    }

                    if (!result.Success)
                    {
                        // todo: add diagnostics
                        throw new CompilationException(GetErrors(result.Diagnostics));
                    }

                    Assembly assembly;
                    ms.Seek(0, SeekOrigin.Begin);

                    if (PlatformHelper.IsMono)
                    {
                        assembly = _loader.LoadStream(ms, pdbStream: null);
                    }
                    else
                    {
                        pdb.Seek(0, SeekOrigin.Begin);
                        assembly = _loader.LoadStream(ms, pdb);
                    }

                    return assembly;
                }
            }
        }

        private List<MetadataReference> GetReferences(IEnumerable<string> names)
        {
            var references = new List<MetadataReference>();

            foreach (var export in names.Select(_libraryManager.GetLibraryExport))
            {
                foreach (var metadataReference in export.MetadataReferences)
                {
                    references.Add(ConvertMetadataReference(metadataReference));
                    //var fileMetadataReference = metadataReference as IMetadataFileReference;

                    //if (fileMetadataReference != null)
                    //{
                    //    references.Add(CreateMetadataFileReference(fileMetadataReference.Path));
                    //}
                    //else
                    //{
                    //    var roslynReference = metadataReference as IRoslynMetadataReference;

                    //    if (roslynReference != null)
                    //    {
                    //        references.Add(roslynReference.MetadataReference);
                    //    }
                    //    else
                    //    {
                    //        throw new NotSupportedException();
                    //    }
                    //}
                }
            }

            return references;
        }

        private static MetadataReference CreateMetadataFileReference(string path)
        {
            return _metadataFileCache.GetOrAdd(path, _ =>
            {
                // TODO: What about access to the file system? We need to be able to 
                // read files from anywhere on disk, not just under the web root
                using (var stream = File.OpenRead(path))
                {
                    return new MetadataImageReference(stream);
                }
            });
        }

        private static SyntaxTree CreateSyntaxTree(string sourcePath, CSharpParseOptions parseOptions)
        {
            using (var stream = File.OpenRead(sourcePath))
            {
                var sourceText = SourceText.From(stream, encoding: Encoding.UTF8);

                return CSharpSyntaxTree.ParseText(sourceText, options: parseOptions, path: sourcePath);
            }
        }

        private static MetadataReference ConvertMetadataReference(IMetadataReference metadataReference)
        {
            var roslynReference = metadataReference as IRoslynMetadataReference;

            if (roslynReference != null)
            {
                return roslynReference.MetadataReference;
            }

            var embeddedReference = metadataReference as IMetadataEmbeddedReference;

            if (embeddedReference != null)
            {
                return new MetadataImageReference(embeddedReference.Contents);
            }

            var fileMetadataReference = metadataReference as IMetadataFileReference;

            if (fileMetadataReference != null)
            {
                
                return CreateMetadataFileReference(fileMetadataReference.Path);
            }

            var projectReference = metadataReference as IMetadataProjectReference;
            if (projectReference != null)
            {
                using (var ms = new MemoryStream())
                {
                    projectReference.EmitReferenceAssembly(ms);

                    ms.Seek(0, SeekOrigin.Begin);

                    return new MetadataImageReference(ms);
                }
            }

            throw new NotSupportedException();
        }

        private static bool IsError(Diagnostic diagnostic)
        {
            return diagnostic.IsWarningAsError || diagnostic.Severity == DiagnosticSeverity.Error;
        }

        private static IList<string> GetErrors(IEnumerable<Diagnostic> diagnostis)
        {
            var formatter = new DiagnosticFormatter();

            return diagnostis.Where(IsError).Select(d => formatter.Format(d)).ToList();
        }
    }
}