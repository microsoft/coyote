// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Microsoft.Coyote.IO;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Microsoft.Coyote.Rewriting
{
    /// <summary>
    /// A pass that logs all assembly contents into text.
    /// </summary>
    internal class LoggingPass : Pass
    {
        /// <summary>
        /// Map from assemblies to IL contents.
        /// </summary>
        private Dictionary<string, AssemblyContents> ContentMap;

        /// <summary>
        /// Initializes a new instance of the <see cref="LoggingPass"/> class.
        /// </summary>
        internal LoggingPass(IEnumerable<AssemblyInfo> visitedAssemblies, ILogger logger)
            : base(visitedAssemblies, logger)
        {
            this.ContentMap = new Dictionary<string, AssemblyContents>();
        }

        /// <inheritdoc/>
        internal override void VisitAssembly(AssemblyDefinition assembly)
        {
            if (!this.ContentMap.ContainsKey(assembly.FullName))
            {
                var contents = new AssemblyContents()
                {
                    AssemblyName = assembly.FullName,
                    Modules = new List<ModuleContents>()
                };

                this.ContentMap.Add(assembly.FullName, contents);
            }

            base.VisitAssembly(assembly);
        }

        /// <inheritdoc/>
        internal override void VisitModule(ModuleDefinition module)
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
        internal override void VisitType(TypeDefinition type)
        {
            if (this.ContentMap.TryGetValue(this.Assembly.FullName, out AssemblyContents contents))
            {
                contents.Modules.FirstOrDefault(m => m.FileName == this.Module.FileName)?.Types.Add(
                    new TypeContents()
                    {
                        Type = type.FullName,
                        Fields = new List<string>(),
                        Methods = new List<MethodContents>()
                    });
            }

            base.VisitType(type);
        }

        /// <inheritdoc/>
        internal override void VisitField(FieldDefinition field)
        {
            if (this.ContentMap.TryGetValue(this.Assembly.FullName, out AssemblyContents contents))
            {
                contents.Modules.FirstOrDefault(m => m.FileName == this.Module.FileName)?.Types
                    .FirstOrDefault(t => t.Type == this.TypeDef.FullName)?.Fields.Add(field.FullName);
            }

            base.VisitField(field);
        }

        /// <inheritdoc/>
        internal override void VisitMethod(MethodDefinition method)
        {
            if (this.ContentMap.TryGetValue(this.Assembly.FullName, out AssemblyContents contents))
            {
                contents.Modules.FirstOrDefault(m => m.FileName == this.Module.FileName)?.Types
                    .FirstOrDefault(t => t.Type == this.TypeDef.FullName)?.Methods
                    .Add(new MethodContents()
                    {
                        Method = method.FullName,
                        Variables = new List<string>(),
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
                    .FirstOrDefault(t => t.Type == this.TypeDef.FullName)?.Methods
                    .FirstOrDefault(m => m.Method == this.Method.FullName)?.Variables
                    .Add(variable.ToString());
            }

            base.VisitVariable(variable);
        }

        /// <inheritdoc/>
        protected override Instruction VisitInstruction(Instruction instruction)
        {
            if (this.ContentMap.TryGetValue(this.Assembly.FullName, out AssemblyContents contents))
            {
                contents.Modules.FirstOrDefault(m => m.FileName == this.Module.FileName)?.Types
                    .FirstOrDefault(t => t.Type == this.TypeDef.FullName)?.Methods
                    .FirstOrDefault(m => m.Method == this.Method.FullName)?.Instructions
                    .Add(instruction.ToString());
            }

            return base.VisitInstruction(instruction);
        }

        /// <summary>
        /// Returns the contents of the specified assembly as JSON.
        /// </summary>
        internal string GetJSON(AssemblyInfo assembly)
        {
            try
            {
                if (this.ContentMap.TryGetValue(assembly.Definition.FullName, out AssemblyContents contents))
                {
                    return JsonSerializer.Serialize(contents, new JsonSerializerOptions()
                    {
                        WriteIndented = true
                    });
                }
            }
            catch (Exception ex)
            {
                this.Logger.WriteLine(LogSeverity.Error, $"Unable to serialize IL to JSON. {ex.Message}");
            }

            return string.Empty;
        }

        private class AssemblyContents
        {
            public string AssemblyName { get; set; }
            public IList<ModuleContents> Modules { get; set; }
        }

        private class ModuleContents
        {
            public string FileName { get; set; }
            public IList<TypeContents> Types { get; set; }
        }

        private class TypeContents
        {
            public string Type { get; set; }
            public IList<string> Fields { get; set; }
            public IList<MethodContents> Methods { get; set; }
        }

        private class MethodContents
        {
            public string Method { get; set; }
            public IList<string> Variables { get; set; }
            public IList<string> Instructions { get; set; }
        }
    }
}
