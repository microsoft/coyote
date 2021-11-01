// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Coyote.IO;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Microsoft.Coyote.Rewriting
{
    /// <summary>
    /// A pass that diffs the IL contents of assemblies and returns them as JSON.
    /// </summary>
    internal class AssemblyDiffingPass : Pass
    {
        /// <summary>
        /// Map from assemblies to IL contents.
        /// </summary>
        private Dictionary<string, AssemblyContents> ContentMap;

        /// <summary>
        /// Initializes a new instance of the <see cref="AssemblyDiffingPass"/> class.
        /// </summary>
        internal AssemblyDiffingPass(IEnumerable<AssemblyInfo> visitedAssemblies, ILogger logger)
            : base(visitedAssemblies, logger)
        {
            this.ContentMap = new Dictionary<string, AssemblyContents>();
        }

        /// <inheritdoc/>
        protected internal override void VisitAssembly(AssemblyDefinition assembly)
        {
            if (!this.ContentMap.ContainsKey(assembly.FullName))
            {
                var contents = new AssemblyContents()
                {
                    FullName = assembly.FullName,
                    Modules = new List<ModuleContents>()
                };

                this.ContentMap.Add(assembly.FullName, contents);
            }

            base.VisitAssembly(assembly);
        }

        /// <inheritdoc/>
        protected internal override void VisitModule(ModuleDefinition module)
        {
            if (this.ContentMap.TryGetValue(this.Assembly.FullName, out AssemblyContents contents))
            {
                contents.Modules.Add(new ModuleContents()
                {
                    FileName = module.FileName,
                    Types = new List<TypeContents>()
                });
            }

            base.VisitModule(module);
        }

        /// <inheritdoc/>
        protected internal override void VisitType(TypeDefinition type)
        {
            if (this.ContentMap.TryGetValue(this.Assembly.FullName, out AssemblyContents contents))
            {
                contents.Modules.FirstOrDefault(m => m.FileName == this.Module.FileName)?.Types.Add(
                    new TypeContents()
                    {
                        FullName = type.FullName,
                        Methods = new List<MethodContents>()
                    });
            }

            base.VisitType(type);
        }

        /// <inheritdoc/>
        protected internal override void VisitField(FieldDefinition field)
        {
            if (this.ContentMap.TryGetValue(this.Assembly.FullName, out AssemblyContents contents))
            {
                contents.Modules.FirstOrDefault(m => m.FileName == this.Module.FileName)?.Types
                    .FirstOrDefault(t => t.FullName == this.TypeDef.FullName)?
                    .AddField(field);
            }

            base.VisitField(field);
        }

        /// <inheritdoc/>
        protected internal override void VisitMethod(MethodDefinition method)
        {
            if (this.ContentMap.TryGetValue(this.Assembly.FullName, out AssemblyContents contents))
            {
                contents.Modules.FirstOrDefault(m => m.FileName == this.Module.FileName)?.Types
                    .FirstOrDefault(t => t.FullName == this.TypeDef.FullName)?.Methods
                    .Add(new MethodContents()
                    {
                        Index = this.TypeDef.Methods.IndexOf(method),
                        Name = method.Name,
                        FullName = method.FullName,
                        Instructions = new List<string>()
                    });
            }

            base.VisitMethod(method);
        }

        /// <inheritdoc/>
        protected override void VisitVariable(VariableDefinition variable)
        {
            if (this.ContentMap.TryGetValue(this.Assembly.FullName, out AssemblyContents contents))
            {
                contents.Modules.FirstOrDefault(m => m.FileName == this.Module.FileName)?.Types
                    .FirstOrDefault(t => t.FullName == this.TypeDef.FullName)?.Methods
                    .FirstOrDefault(m => m.FullName == this.Method.FullName)?
                    .AddVariable(variable);
            }

            base.VisitVariable(variable);
        }

        /// <inheritdoc/>
        protected override Instruction VisitInstruction(Instruction instruction)
        {
            if (this.ContentMap.TryGetValue(this.Assembly.FullName, out AssemblyContents contents))
            {
                contents.Modules.FirstOrDefault(m => m.FileName == this.Module.FileName)?.Types
                    .FirstOrDefault(t => t.FullName == this.TypeDef.FullName)?.Methods
                    .FirstOrDefault(m => m.FullName == this.Method.FullName)?.Instructions
                    .Add(instruction.ToString());
            }

            return base.VisitInstruction(instruction);
        }

        /// <summary>
        /// Returns the diff between the IL contents of this pass against the specified pass as JSON.
        /// </summary>
        internal string GetDiffJson(AssemblyInfo assembly, AssemblyDiffingPass pass)
        {
            if (!this.ContentMap.TryGetValue(assembly.FullName, out AssemblyContents thisContents) ||
                !pass.ContentMap.TryGetValue(assembly.FullName, out AssemblyContents otherContents))
            {
                this.Logger.WriteLine(LogSeverity.Error, "Unable to diff IL code that belongs to different assemblies.");
                return string.Empty;
            }

            // Diff contents of the two assemblies.
            var diffedContents = thisContents.Diff(otherContents);
            return this.GetJson(diffedContents);
        }

        /// <summary>
        /// Returns the IL contents of the specified assembly as JSON.
        /// </summary>
        internal string GetJson(AssemblyInfo assembly) =>
            this.ContentMap.TryGetValue(assembly.Definition.FullName, out AssemblyContents contents) ?
            this.GetJson(contents) : string.Empty;

        /// <summary>
        /// Returns the IL contents of the specified assembly as JSON.
        /// </summary>
        private string GetJson(AssemblyContents contents)
        {
            try
            {
                contents.Resolve();
                return JsonSerializer.Serialize(contents, new JsonSerializerOptions()
                {
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                    WriteIndented = true
                });
            }
            catch (Exception ex)
            {
                this.Logger.WriteLine(LogSeverity.Error, $"Unable to serialize IL to JSON. {ex.Message}");
            }

            return string.Empty;
        }

        /// <summary>
        /// The status of diffing two IL contents.
        /// </summary>
        private enum DiffStatus
        {
            /// <summary>
            /// Contents are identical.
            /// </summary>
            None,

            /// <summary>
            /// Contents have been added.
            /// </summary>
            Added,

            /// <summary>
            /// Contents have been removed.
            /// </summary>
            Removed
        }

        private class AssemblyContents
        {
            public string FullName { get; set; }
            public List<ModuleContents> Modules { get; set; }

            /// <summary>
            /// Returns the diff between the IL contents of this assembly against the specified assembly.
            /// </summary>
            internal AssemblyContents Diff(AssemblyContents other)
            {
                var diffedContents = new AssemblyContents()
                {
                    FullName = this.FullName,
                    Modules = new List<ModuleContents>()
                };

                var diffedModules = this.Modules.Union(other.Modules);
                foreach (var module in diffedModules)
                {
                    var thisModule = this.Modules.FirstOrDefault(m => m.Equals(module));
                    var otherModule = other.Modules.FirstOrDefault(m => m.Equals(module));
                    if (thisModule is null)
                    {
                        diffedContents.Modules.Add(module.Clone(DiffStatus.Added));
                    }
                    else if (otherModule is null)
                    {
                        diffedContents.Modules.Add(module.Clone(DiffStatus.Removed));
                    }
                    else
                    {
                        diffedContents.Modules.Add(thisModule.Diff(otherModule));
                    }
                }

                return diffedContents;
            }

            internal void Resolve()
            {
                this.Modules.ForEach(m => m.Resolve());
                this.Modules.RemoveAll(t => t.Types is null);
                this.Modules = this.Modules.Count is 0 ? null : this.Modules;
            }

            public override bool Equals(object obj) => obj is AssemblyContents other && this.FullName == other.FullName;
            public override int GetHashCode() => this.FullName.GetHashCode();
        }

        private class ModuleContents
        {
            public string FileName { get; set; }
            public List<TypeContents> Types { get; set; }

            [JsonIgnore]
            internal DiffStatus DiffStatus { get; private set; } = DiffStatus.None;

            /// <summary>
            /// Returns the diff between the IL contents of this module against the specified module.
            /// </summary>
            internal ModuleContents Diff(ModuleContents other)
            {
                var diffedContents = new ModuleContents()
                {
                    FileName = this.FileName,
                    Types = new List<TypeContents>()
                };

                var diffedTypes = this.Types.Union(other.Types);
                foreach (var type in diffedTypes)
                {
                    var thisType = this.Types.FirstOrDefault(t => t.Equals(type));
                    var otherType = other.Types.FirstOrDefault(t => t.Equals(type));
                    if (thisType is null)
                    {
                        diffedContents.Types.Add(type.Clone(DiffStatus.Added));
                    }
                    else if (otherType is null)
                    {
                        diffedContents.Types.Add(type.Clone(DiffStatus.Removed));
                    }
                    else
                    {
                        diffedContents.Types.Add(thisType.Diff(otherType));
                    }
                }

                return diffedContents;
            }

            internal ModuleContents Clone(DiffStatus status)
            {
                string prefix = status is DiffStatus.Added ? "[+] " : status is DiffStatus.Removed ? "[-] " : string.Empty;
                return new ModuleContents()
                {
                    FileName = prefix + this.FileName,
                    Types = this.Types.Select(t => t.Clone(DiffStatus.None)).ToList(),
                    DiffStatus = status
                };
            }

            internal void Resolve()
            {
                this.Types.ForEach(t => t.Resolve());
                this.Types.RemoveAll(t => t.Fields is null && t.Methods is null);
                this.Types = this.Types.Count is 0 ? null : this.Types;
            }

            public override bool Equals(object obj) => obj is ModuleContents other && this.FileName == other.FileName;
            public override int GetHashCode() => this.FileName.GetHashCode();
        }

        private class TypeContents
        {
            public string FullName { get; set; }
            public IEnumerable<string> Fields { get; set; }
            public List<MethodContents> Methods { get; set; }

            private List<(string, string)> FieldContents = new List<(string, string)>();

            [JsonIgnore]
            internal DiffStatus DiffStatus { get; private set; } = DiffStatus.None;

            internal void AddField(FieldDefinition field)
            {
                this.FieldContents.Add((field.Name, field.ToString()));
            }

            /// <summary>
            /// Returns the diff between the IL contents of this type against the specified type.
            /// </summary>
            internal TypeContents Diff(TypeContents other)
            {
                var diffedContents = new TypeContents()
                {
                    FullName = this.FullName,
                    Methods = new List<MethodContents>()
                };

                var diffedFields = this.FieldContents.Union(other.FieldContents);
                foreach (var field in diffedFields)
                {
                    var (thisField, thisType) = this.FieldContents.FirstOrDefault(f => f == field);
                    var (otherField, otherType) = other.FieldContents.FirstOrDefault(f => f == field);
                    if (thisField is null)
                    {
                        diffedContents.FieldContents.Add((field.Item1, "[+] " + field.Item2));
                    }
                    else if (otherField is null)
                    {
                        diffedContents.FieldContents.Add((field.Item1, "[-] " + field.Item2));
                    }
                }

                var diffedMethods = this.Methods.Union(other.Methods);
                foreach (var method in diffedMethods)
                {
                    var thisMethod = this.Methods.FirstOrDefault(m => m.Equals(method));
                    var otherMethod = other.Methods.FirstOrDefault(m => m.Equals(method));
                    if (thisMethod is null)
                    {
                        diffedContents.Methods.Add(method.Clone(DiffStatus.Added));
                    }
                    else if (otherMethod is null)
                    {
                        diffedContents.Methods.Add(method.Clone(DiffStatus.Removed));
                    }
                    else
                    {
                        diffedContents.Methods.Add(thisMethod.Diff(otherMethod));
                    }
                }

                return diffedContents;
            }

            internal TypeContents Clone(DiffStatus status)
            {
                string prefix = status is DiffStatus.Added ? "[+] " : status is DiffStatus.Removed ? "[-] " : string.Empty;
                return new TypeContents()
                {
                    FullName = prefix + this.FullName,
                    FieldContents = this.FieldContents.ToList(),
                    Methods = this.Methods.Select(m => m.Clone(DiffStatus.None)).ToList(),
                    DiffStatus = status
                };
            }

            internal void Resolve()
            {
                this.FieldContents = this.FieldContents.Count is 0 ? null : this.FieldContents;
                if (this.FieldContents != null)
                {
                    this.Fields = this.FieldContents.OrderBy(kvp => kvp.Item1).Select(kvp => kvp.Item2);
                }

                this.Methods.ForEach(m => m.Resolve());
                this.Methods.RemoveAll(m => m.Variables is null && m.Instructions is null);
                this.Methods = this.Methods.Count is 0 ? null : this.Methods;
            }

            public override bool Equals(object obj) => obj is TypeContents other && this.FullName == other.FullName;
            public override int GetHashCode() => this.FullName.GetHashCode();
        }

        private class MethodContents
        {
            [JsonIgnore]
            internal int Index { get; set; }

            public string Name { get; set; }
            public string FullName { get; set; }
            public IEnumerable<string> Variables { get; set; }
            public List<string> Instructions { get; set; }

            private List<(int, string)> VariableContents = new List<(int, string)>();

            [JsonIgnore]
            internal DiffStatus DiffStatus { get; private set; } = DiffStatus.None;

            internal void AddVariable(VariableDefinition variable)
            {
                this.VariableContents.Add((variable.Index, $"[{variable.Index}] {variable.VariableType.FullName}"));
            }

            /// <summary>
            /// Returns the diff between the IL contents of this method against the specified method.
            /// </summary>
            internal MethodContents Diff(MethodContents other)
            {
                // TODO: show diff in method signature.
                var diffedContents = new MethodContents()
                {
                    Index = this.Index,
                    Name = this.Name,
                    FullName = other.FullName,
                    Instructions = new List<string>()
                };

                var diffedVariables = this.VariableContents.Union(other.VariableContents);
                foreach (var variable in diffedVariables)
                {
                    var (thisVariable, thisType) = this.VariableContents.FirstOrDefault(v => v == variable);
                    var (otherVariable, otherType) = other.VariableContents.FirstOrDefault(v => v == variable);
                    if (thisType is null)
                    {
                        diffedContents.VariableContents.Add((variable.Item1, "[+] " + variable.Item2));
                    }
                    else if (otherType is null)
                    {
                        diffedContents.VariableContents.Add((variable.Item1, "[-] " + variable.Item2));
                    }
                }

                var diffedInstructions = this.Instructions.Union(other.Instructions);
                foreach (var instruction in diffedInstructions)
                {
                    var thisInstruction = this.Instructions.FirstOrDefault(i => i == instruction);
                    var otherInstruction = other.Instructions.FirstOrDefault(i => i == instruction);
                    if (thisInstruction is null)
                    {
                        diffedContents.Instructions.Add("[+] " + instruction);
                    }
                    else if (otherInstruction is null)
                    {
                        diffedContents.Instructions.Add("[-] " + instruction);
                    }
                }

                diffedContents.Instructions.Sort((x, y) =>
                {
                    // Find offset of each instruction and convert from hex to int.
                    // The offset in IL is in the form 'IL_004a'.
                    int xOffset = Convert.ToInt32(x.Substring(7, 4), 16);
                    int yOffset = Convert.ToInt32(y.Substring(7, 4), 16);
                    return xOffset == yOffset ? x[1].CompareTo(y[1]) * -1 : xOffset.CompareTo(yOffset);
                });

                return diffedContents;
            }

            internal MethodContents Clone(DiffStatus status)
            {
                string prefix = status is DiffStatus.Added ? "[+] " : status is DiffStatus.Removed ? "[-] " : string.Empty;
                return new MethodContents()
                {
                    Index = this.Index,
                    Name = this.Name,
                    FullName = prefix + this.FullName,
                    VariableContents = this.VariableContents.ToList(),
                    Instructions = this.Instructions.ToList(),
                    DiffStatus = status
                };
            }

            internal void Resolve()
            {
                this.VariableContents = this.VariableContents.Count is 0 ? null : this.VariableContents;
                if (this.VariableContents != null)
                {
                    this.Variables = this.VariableContents.OrderBy(kvp => kvp.Item1).Select(kvp => kvp.Item2);
                }

                this.Instructions = this.Instructions.Count is 0 ? null : this.Instructions;
            }

            public override bool Equals(object obj) => obj is MethodContents other &&
                this.Index == other.Index && this.Name == other.Name;

            public override int GetHashCode()
            {
                unchecked
                {
                    var hash = 19;
                    hash = (hash * 31) + this.Index.GetHashCode();
                    hash = (hash * 31) + this.Name.GetHashCode();
                    return hash;
                }
            }
        }
    }
}
