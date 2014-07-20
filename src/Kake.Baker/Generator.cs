using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Text;
using Microsoft.Framework.Runtime;

namespace Kake
{
    /// <summary>
    /// Summary description for Generator
    /// </summary>
    public static class Generator
    {
        const string BaseClass = "Kake.Module";

        static readonly Regex _r = new Regex(@"-[a-z]", RegexOptions.CultureInvariant);
        static string SaniticeName(string name)
        {
            name = name.Substring(0, 1).ToUpperInvariant() + name.Substring(1);
            name = name.Replace(".", "_");
            name = _r.Replace(name, m => m.Value.Substring(1).ToUpperInvariant());
            return name;
        }

        private static IEnumerable<StatementSyntax> MakeBody(CodeBlock block, CSharpParseOptions options)
        {
            if (block == null)
                return Enumerable.Empty<StatementSyntax>();

            var code = new StringBuilder("async void main() {")
                .AppendLine()
                .Append("#line ")
                .Append(block.StartLine)
                .AppendLine()
                .Append(block.Code)
                .AppendLine()
                .Append("}")
                .ToString();

            var parsed = SyntaxFactory.ParseCompilationUnit(code, options: options);
            var nodes = parsed.DescendantNodes(n => !n.IsKind(SyntaxKind.Block)).Single(n => n.IsKind(SyntaxKind.Block)).ChildNodes().Cast<StatementSyntax>()
                .ToList();
            if (nodes.Count > 0)
            {
                nodes[0] = nodes[0].WithLeadingTrivia(
                    nodes[0].GetLeadingTrivia().Insert(0,
                        SyntaxFactory.SyntaxTrivia(SyntaxKind.WhitespaceTrivia, Environment.NewLine)
                    ).Add(
                        SyntaxFactory.SyntaxTrivia(SyntaxKind.WhitespaceTrivia, Environment.NewLine)
                    )
                );
            }
            return nodes;
        }

        public static IEnumerable<Meta> Extract(ref IEnumerable<Meta> all, string name)
        {
            var copy = all;
            all = copy.Where(m => m.Name != name);
            return copy.Where(m => m.Name == name);
        }

        public static Assembly Generate(
            string file,
            KakeUnit unit,
            IApplicationEnvironment applicationEnvironment,
            ILibraryExportProvider libraryExportProvider,
            IAssemblyLoaderEngine assemblyLoaderEngine)
        {
            var options = new CSharpParseOptions(LanguageVersion.Experimental, kind: SourceCodeKind.Regular);

            var name = Path.GetFileNameWithoutExtension(file);
            var className = SaniticeName(name);
            IEnumerable<Meta> meta = unit.Meta;
            var usings = Extract(ref meta, "using");
            var loads = Extract(ref meta, "load");

            var statements = ImmutableList.CreateBuilder<StatementSyntax>();
            foreach (var m in meta.Where(m => m.Name == "include").Concat(meta.Where(m => m.Name != "include")))
            {
                var invoke = SyntaxFactory.InvocationExpression(
                    SyntaxFactory.IdentifierName(SaniticeName(m.Name)),
                    SyntaxFactory.ArgumentList().WithArguments(
                        SyntaxFactory.SeparatedList(
                            m.Args.Select(a =>
                                SyntaxFactory.Argument(
                                    SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression)
                                        .WithToken(SyntaxFactory.Literal(a))
                                )
                            )
                        )
                    )
                );
                statements.Add(SyntaxFactory.ExpressionStatement(invoke).NormalizeWhitespace("  "));
            }

            statements.AddRange(MakeBody(unit.Code, options));

            foreach (var target in unit.Targets)
            {
                var invoke = SyntaxFactory.InvocationExpression(
                    SyntaxFactory.IdentifierName("Target"),
                    SyntaxFactory.ArgumentList().WithArguments(
                        SyntaxFactory.SeparatedList(new[] {
                            SyntaxFactory.Argument(
                                SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression)
                                    .WithToken(SyntaxFactory.Literal(target.Name))
                            ),
                            SyntaxFactory.Argument(
                                SyntaxFactory.ParenthesizedLambdaExpression(
                                    SyntaxFactory.Token(SyntaxKind.AsyncKeyword),
                                    SyntaxFactory.ParameterList(),
                                    SyntaxFactory.Token(SyntaxKind.EqualsGreaterThanToken),
                                    SyntaxFactory.Block(MakeBody(target.Code, options))
                                )
                            )
                        })
                    )
                );

                foreach (var m in target.Meta)
                {
                    invoke = SyntaxFactory.InvocationExpression(
                        SyntaxFactory.MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            invoke,
                            SyntaxFactory.IdentifierName(SaniticeName(m.Name))
                        ),
                        SyntaxFactory.ArgumentList().WithArguments(
                            SyntaxFactory.SeparatedList(
                                m.Args.Select(a =>
                                    SyntaxFactory.Argument(
                                        SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression)
                                            .WithToken(SyntaxFactory.Literal(a))
                                    )
                                )
                            )
                        ).NormalizeWhitespace("  ")
                    );
                }

                statements.Add(SyntaxFactory.ExpressionStatement(invoke));
            }

            var syntax = SyntaxFactory.CompilationUnit()
                .WithUsings(
                    SyntaxFactory.List(
                        usings.SelectMany(u => u.Args).Select(u =>
                            SyntaxFactory.UsingDirective(
                                SyntaxFactory.IdentifierName(u)
                            )
                        )
                    )
                )
                .WithMembers(
                    SyntaxFactory.SingletonList<MemberDeclarationSyntax>(
                        SyntaxFactory.ClassDeclaration(
                            className)
                        .WithBaseList(
                            SyntaxFactory.BaseList(
                                SyntaxFactory.SingletonSeparatedList<TypeSyntax>(
                                    SyntaxFactory.IdentifierName(
                                        BaseClass))))
                        .WithMembers(
                            SyntaxFactory.SingletonList<MemberDeclarationSyntax>(
                                SyntaxFactory.ConstructorDeclaration(
                                    SyntaxFactory.Identifier(
                                        className))
                                .WithModifiers(
                                    SyntaxFactory.TokenList(
                                        SyntaxFactory.Token(
                                            SyntaxKind.PublicKeyword)))
                                .WithInitializer(
                                    SyntaxFactory.ConstructorInitializer(
                                        SyntaxKind.BaseConstructorInitializer,
                                        SyntaxFactory.ArgumentList()
                                        .WithOpenParenToken(
                                            SyntaxFactory.MissingToken(
                                                SyntaxKind.OpenParenToken))
                                        .WithCloseParenToken(
                                            SyntaxFactory.MissingToken(
                                                SyntaxKind.CloseParenToken))))
                                .WithBody(
                                    SyntaxFactory.Block(statements.ToImmutable()))))));

            var syntaxTree = SyntaxFactory.SyntaxTree(syntax, path: file, encoding: Encoding.UTF8);

            var settings = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);
            var compilation = CSharpCompilation.Create(name)
                .WithOptions(settings)
                .AddSyntaxTrees(syntaxTree);

            var refs = loads.SelectMany(l => l.Args)
                .Concat(new[] { "Kake" })
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Select(a => libraryExportProvider.GetLibraryExport(a, applicationEnvironment.TargetFramework, applicationEnvironment.Configuration));

            foreach (var r in refs)
            {
                foreach (var sourceReference in r.SourceReferences)
                {
                    var sourceFileReference = sourceReference as ISourceFileReference;
                    if (sourceFileReference != null)
                    {
                        var sourcePath = sourceFileReference.Path;
                        compilation = compilation.AddSyntaxTrees(CreateSyntaxTree(sourcePath, options));
                    }
                }

                foreach (var metadataReference in r.MetadataReferences)
                {
                    var reference = ConvertMetadataReference(metadataReference);
                    compilation = compilation.AddReferences(reference);
                }
            }

            using (var pdbStream = new MemoryStream())
            using (var assemblyStream = new MemoryStream())
            {
                EmitResult result = null;

                if (PlatformHelper.IsMono)
                {
                    result = compilation.Emit(assemblyStream);
                }
                else
                {
                    result = compilation.Emit(assemblyStream, pdbStream: pdbStream);
                }

                if (!result.Success)
                {
                    // todo: add diagnostics
                    throw new CompilationException(GetErrors(result.Diagnostics));
                }

                Assembly assembly = null;

                // Rewind the stream
                assemblyStream.Seek(0, SeekOrigin.Begin);

                if (PlatformHelper.IsMono)
                {
                    // Pdbs aren't supported on mono
                    assembly = assemblyLoaderEngine.LoadStream(assemblyStream, pdbStream: null);
                }
                else
                {
                    // Rewind the pdb stream
                    pdbStream.Seek(0, SeekOrigin.Begin);

                    assembly = assemblyLoaderEngine.LoadStream(assemblyStream, pdbStream);
                }

                return assembly;
            }
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
                return new MetadataFileReference(fileMetadataReference.Path);
            }

            var projectReference = metadataReference as IMetadataProjectReference;
            if (projectReference != null)
            {
                using (var ms = new MemoryStream())
                {
                    projectReference.WriteReferenceAssemblyStream(ms);

                    ms.Seek(0, SeekOrigin.Begin);

                    return new MetadataImageReference(ms);
                }
            }

            throw new NotSupportedException();
        }

        private static IList<string> GetErrors(IEnumerable<Diagnostic> diagnostis)
        {
            var formatter = new DiagnosticFormatter();

            return diagnostis.Select(d => formatter.Format(d)).ToList();
        }
    }
}