// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

#if NET46
using System.ServiceModel;

using Microsoft.Coyote.TestingServices.Coverage;
#endif

namespace Microsoft.Coyote.TestingServices
{
    /// <summary>
    /// Interface for a remote testing process.
    /// </summary>
#if NET46
    [ServiceContract(Namespace = "Microsoft.Coyote")]
    [ServiceKnownType(typeof(TestReport))]
    [ServiceKnownType(typeof(CoverageInfo))]
    [ServiceKnownType(typeof(Transition))]
#endif
    internal interface ITestingProcess
    {
        /// <summary>
        /// Returns the test report.
        /// </summary>
#if NET46
        [OperationContract]
#endif
        TestReport GetTestReport();

        /// <summary>
        /// Stops testing.
        /// </summary>
#if NET46
        [OperationContract]
#endif
        void Stop();
    }
}
