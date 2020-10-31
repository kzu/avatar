﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace Avatars.UnitTests
{
    /// <summary>
    /// Runs all the scenarios in the Scenarios folder using the source 
    /// generator to process them.
    /// </summary>
    public class Scenarios
    {
        [Theory]
        [MemberData(nameof(GetScenarios))]
        public void Run(string path)
        {
            var (diagnostics, compilation) = GetGeneratedOutput(path);

            Assert.Empty(diagnostics);

            var assembly = compilation.Emit();
            var type = assembly.GetTypes().FirstOrDefault(t => typeof(IRunnable).IsAssignableFrom(t));

            Assert.NotNull(type);

            var runnable = (IRunnable)Activator.CreateInstance(type);
            runnable.Run();
        }

        public static IEnumerable<object[]> GetScenarios()
            => typeof(ThisAssembly.Constants.Scenarios).GetFields()
                .Select(f => new object[] { f.GetValue(null) });

        static (ImmutableArray<Diagnostic>, Compilation) GetGeneratedOutput(string path)
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(File.ReadAllText(path), path: path);

            // Force-load this core assembly;
            new AvatarGeneratorAttribute();
            var references = new List<MetadataReference>();
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in assemblies.Where(x => !x.IsDynamic && !string.IsNullOrEmpty(x.Location)))
                references.Add(MetadataReference.CreateFromFile(assembly.Location));

            var compilation = CSharpCompilation.Create(Path.GetFileNameWithoutExtension(path),
                new SyntaxTree[]
                {
                    syntaxTree,
                    CSharpSyntaxTree.ParseText(File.ReadAllText("Avatar/Avatar.cs"), path: "Avatar.cs"),
                    CSharpSyntaxTree.ParseText(File.ReadAllText("Avatar/Avatar.StaticFactory.cs"), path: "Avatar.StaticFactory.cs"),
                }, references, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            var diagnostics = compilation.GetDiagnostics().RemoveAll(d => 
                d.Severity == DiagnosticSeverity.Hidden || 
                d.Severity == DiagnosticSeverity.Info || 
                // Type conflicts with referenced assembly, will happen because scenarios 
                // are also compiled in the unit test project itself, but also in the scenario 
                // file compilation, but the locally defined in surce wins.
                d.Id == "CS0436");

            if (diagnostics.Any())
                return (diagnostics, compilation);

            ISourceGenerator generator = new AvatarSourceGenerator();

            var driver = CSharpGeneratorDriver.Create(generator);
            driver.RunGeneratorsAndUpdateCompilation(compilation, out var output, out diagnostics);

            return (diagnostics, output);
        }
    }
}
