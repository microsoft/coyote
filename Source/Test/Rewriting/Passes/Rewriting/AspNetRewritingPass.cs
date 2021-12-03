// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#if NET || NETCOREAPP3_1
using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Coyote.IO;
using Microsoft.Coyote.Rewriting.Types;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Microsoft.Coyote.Rewriting
{
    internal class AspNetRewritingPass : RewritingPass
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AspNetRewritingPass"/> class.
        /// </summary>
        internal AspNetRewritingPass(IEnumerable<AssemblyInfo> visitedAssemblies, ILogger logger)
            : base(visitedAssemblies, logger)
        {
        }

        /// <inheritdoc/>
        protected internal override void VisitMethod(MethodDefinition method)
        {
            bool isControllerMethod = false;
            if (method.CustomAttributes.Count > 0)
            {
                // Search for a method with a unit testing framework attribute.
                foreach (var attr in method.CustomAttributes)
                {
                    // if (attr.AttributeType.FullName == "Microsoft.VisualStudio.TestTools.UnitTesting.TestMethodAttribute")
                    // {
                    //     isTestMethod = true;
                    //     break;
                    // }
                }
            }

            if (isControllerMethod)
            {
                // Debug.WriteLine($"............. [-] test method '{method.Name}'");

                // MethodDefinition newMethod = CloneMethod(method);
                // this.RewriteTestMethod(method, newMethod);

                // method.DeclaringType.Methods.Add(newMethod);

                // Debug.WriteLine($"............. [+] systematic test method '{method.Name}'");
                // Debug.WriteLine($"............. [+] test method '{newMethod.Name}'");
            }

            base.VisitMethod(method);
        }
    }
}
#endif
