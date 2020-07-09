// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Coyote.IO;
using Mono.Cecil;
using Mono.Cecil.Cil;
using CoyoteTasks = Microsoft.Coyote.Tasks;

namespace Microsoft.Coyote.Rewriting
{
    /// <summary>
    /// Rewrites an assembly for systematic testing.
    /// </summary>
    internal class AssemblyRewriter
    {
        /// <summary>
        /// Temporary directory that is used to write the rewritten assemblies
        /// in the case that they are replacing the original ones.
        /// </summary>
        /// <remarks>
        /// We need this because it seems Mono.Cecil does not allow to rewrite in-place.
        /// </remarks>
        private const string TempDirectory = "__temp_coyote__";

        /// <summary>
        /// Configuration for rewriting assemblies.
        /// </summary>
        private readonly Configuration Configuration;

        /// <summary>
        /// List of transforms we are applying while rewriting.
        /// </summary>
        private readonly List<AssemblyTransform> Transforms = new List<AssemblyTransform>();

        /// <summary>
        /// Initializes a new instance of the <see cref="AssemblyRewriter"/> class.
        /// </summary>
        /// <param name="configuration">The configuration for this rewriter.</param>
        private AssemblyRewriter(Configuration configuration)
        {
            this.Configuration = configuration;
            this.Transforms.Add(new TaskTransform());
            this.Transforms.Add(new LockTransform());
        }

        /// <summary>
        /// Rewrites the assemblies specified in the configuration file.
        /// </summary>
        internal static void Rewrite(string configurationFile) => Rewrite(Configuration.ParseFromJSON(configurationFile));

        /// <summary>
        /// Rewrites the assemblies specified in the configuration.
        /// </summary>
        internal static void Rewrite(Configuration configuration)
        {
            var binaryRewriter = new AssemblyRewriter(configuration);
            binaryRewriter.Rewrite();
        }

        /// <summary>
        /// Performs the rewriting.
        /// </summary>
        private void Rewrite()
        {
            string coyotePath = typeof(CoyoteTasks.AsyncTaskMethodBuilder).Assembly.Location;
            string outputDirectory = this.Configuration.IsReplacingAssemblies ?
                Path.Combine(this.Configuration.OutputDirectory, TempDirectory) : this.Configuration.OutputDirectory;
            Directory.CreateDirectory(outputDirectory);

            // make sure target path also has Microsoft.Coyote.dll
            string coyoteOutputPath = Path.Combine(this.Configuration.OutputDirectory, Path.GetFileName(coyotePath));
            if (!File.Exists(coyoteOutputPath) || File.GetLastWriteTime(coyoteOutputPath) != File.GetLastWriteTime(coyotePath))
            {
                File.Copy(coyotePath, coyoteOutputPath, true);
            }

            // Rewrite the assembly files to the output directory.
            foreach (string assemblyPath in this.Configuration.AssemblyPaths)
            {
                // Specify the search directory for resolving assemblies.
                var assemblyResolver = new DefaultAssemblyResolver();
                assemblyResolver.AddSearchDirectory(this.Configuration.AssembliesDirectory);
                assemblyResolver.AddSearchDirectory(Path.GetDirectoryName(coyotePath));
                assemblyResolver.ResolveFailure += OnResolveAssemblyFailure;

                var assembly = AssemblyDefinition.ReadAssembly(assemblyPath, new ReaderParameters()
                {
                    AssemblyResolver = assemblyResolver,
                    ReadSymbols = true
                });

                string assemblyName = Path.GetFileName(assemblyPath);
                string outputPath = Path.Combine(outputDirectory, assemblyName);

                Console.WriteLine($"... Rewriting the '{assemblyName}' assembly ({assembly.FullName})");
                foreach (var module in assembly.Modules)
                {
                    Debug.WriteLine($"..... Rewriting the '{module.Name}' module ({module.FileName})");
                    this.RewriteModule(module);
                }

                // Write the binary in the output path with portable symbols enabled.
                Console.WriteLine($"... Writing the modified '{assemblyName}' assembly to " +
                    $"{(this.Configuration.IsReplacingAssemblies ? assemblyPath : outputPath)}");
                assembly.Write(outputPath, new WriterParameters()
                {
                    WriteSymbols = true,
                    SymbolWriterProvider = new PortablePdbWriterProvider()
                });

                assembly.Dispose();
                if (this.Configuration.IsReplacingAssemblies)
                {
                    File.Copy(outputPath, assemblyPath, true);
                    string pdbFile = Path.Combine(Path.GetDirectoryName(outputPath), Path.GetFileNameWithoutExtension(outputPath) + ".pdb");
                    string targetPdbFile = Path.Combine(Path.GetDirectoryName(assemblyPath), Path.GetFileNameWithoutExtension(assemblyPath) + ".pdb");
                    File.Copy(pdbFile, targetPdbFile, true);
                }
            }

            if (this.Configuration.IsReplacingAssemblies)
            {
                // Delete the temporary output directory.
                Directory.Delete(outputDirectory, true);
            }
            else
            {
                // Copy the dependency files to the output directory.
                foreach (string dependencyPath in this.Configuration.DependencyPaths)
                {
                    string dependencyName = Path.GetFileName(dependencyPath);
                    Console.WriteLine($"... Copying the '{dependencyName}' dependency file");
                    File.Copy(dependencyPath, Path.Combine(outputDirectory, dependencyName), true);
                }
            }

            Console.WriteLine($". Done rewriting");
        }

        private static AssemblyDefinition OnResolveAssemblyFailure(object sender, AssemblyNameReference reference)
        {
            Console.WriteLine("Error resolving assembly: " + reference.FullName);
            return null;
        }

        /// <summary>
        /// Rewrites the specified module definition.
        /// </summary>
        private void RewriteModule(ModuleDefinition module)
        {
            foreach (var t in this.Transforms)
            {
                t.VisitModule(module);
            }

            foreach (var type in module.GetTypes())
            {
                this.RewriteType(type);
            }
        }

        /// <summary>
        /// Rewrites the specified type definition.
        /// </summary>
        private void RewriteType(TypeDefinition type)
        {
            Debug.WriteLine($"....... Rewriting type '{type.FullName}'");

            foreach (var t in this.Transforms)
            {
                t.VisitType(type);
            }

            foreach (var field in type.Fields)
            {
                foreach (var t in this.Transforms)
                {
                    t.VisitField(field);
                }
            }

            foreach (var method in type.Methods)
            {
                this.RewriteMethod(method);
            }
        }

        /// <summary>
        /// Rewrites the specified method definition.
        /// </summary>
        private void RewriteMethod(MethodDefinition method)
        {
            Debug.WriteLine($"......... Rewriting method '{method.FullName}'");

            foreach (var t in this.Transforms)
            {
                t.VisitMethod(method);
            }

            // Only non-abstract method bodies can be rewritten.
            if (!method.IsAbstract)
            {
                ILProcessor processor = method.Body.GetILProcessor();
                foreach (var variable in method.Body.Variables)
                {
                    foreach (var t in this.Transforms)
                    {
                        t.VisitVariable(variable);
                    }
                }

                // Do exception handlers before the method instructions because they are a
                // higher level concept and it's handy to pre-process them before seeing the
                // raw instructions.
                if (method.Body.HasExceptionHandlers)
                {
                    foreach (var t in this.Transforms)
                    {
                        foreach (var handler in method.Body.ExceptionHandlers)
                        {
                            t.VisitExceptionHandler(handler);
                        }
                    }
                }

                // in this case run each transform as separate passes over the method body
                // so they don't trip over each other's edits.
                foreach (var t in this.Transforms)
                {
                    Instruction instruction = method.Body.Instructions.FirstOrDefault();
                    while (instruction != null)
                    {
                        instruction = t.VisitInstruction(instruction);
                        instruction = instruction.Next;
                    }
                }
            }
        }
    }
}
