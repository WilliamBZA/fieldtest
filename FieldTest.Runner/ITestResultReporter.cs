using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FieldTest.Runner
{
    public interface ITestResultReporter
    {
        void ReportTestResult(string testId, string result, string message);
    }
}