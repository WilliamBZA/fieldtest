using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FieldTest.Core.Model
{
    public enum TestStatus
    {
        NotRun, // NotRun.png
        Changed, // Changed.png
        Passed, // Passed.png
        Failed, // Failed.png
        Inconclusive, // Inconclusive.png

        NotRunPrevious,
        ChangedPrevious,
        PassedPrevious,
        FailedPrevious,
        InconclusivePrevious,

        InProgress
    }
}