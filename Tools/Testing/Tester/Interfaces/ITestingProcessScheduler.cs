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
    /// Interface for a remote testing process scheduler.
    /// </summary>
#if NET46
    [ServiceContract(Namespace = "Microsoft.Coyote")]
    [ServiceKnownType(typeof(TestReport))]
    [ServiceKnownType(typeof(CoverageInfo))]
    [ServiceKnownType(typeof(Transition))]
#endif
    internal interface ITestingProcessScheduler
    {
        /// <summary>
        /// Notifies the testing process scheduler
        /// that a bug was found.
        /// </summary>
#if NET46
        [OperationContract]
#endif
        void NotifyBugFound(uint processId);

        /// <summary>
        /// Sets the test report from the specified process.
        /// </summary>
#if NET46
        [OperationContract]
#endif
        void SetTestReport(TestReport testReport, uint processId);
    }
}
