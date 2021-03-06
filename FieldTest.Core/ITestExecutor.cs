﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FieldTest.Core.Model;

namespace FieldTest.Core
{
    public interface ITestExecutor
    {
        void RunTests(IEnumerable<TestDetails> tests, TestAssembly assembly, string runnerPath, IDebugAttacher debuggerAttacher);
    }
}
