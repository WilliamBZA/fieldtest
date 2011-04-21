using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using NUnit.Core;
using NUnit.Util;
using NUnit.Core.Filters;

namespace FieldTest.Runner
{
    public class NUnitTestFramework : BaseTestFramework
    {
        public NUnitTestFramework(ITestResultReporter testResultReporter)
            : base(testResultReporter)
        {
        }

        public override void InitializeClass(object classInstance, System.Reflection.MethodInfo[] _classMethods)
        {
        }

        public override void CleanupClass(object classInstance, System.Reflection.MethodInfo[] _classMethods)
        {
        }

        public override void InitializeTest(object classInstance, System.Reflection.MethodInfo[] _classMethods)
        {
        }

        public override void CleanupTest(object classInstance, System.Reflection.MethodInfo[] _classMethods)
        {
        }

        public override void ExecuteTest(object instance, string testId, string methodName)
        {
            using (RemoteTestRunner testRunner = new RemoteTestRunner())
            {
                TestPackage package = new TestPackage("FieldTest");
                package.Assemblies.Add(instance.GetType().Assembly.Location); 
                
                testRunner.Load(package);
                var testCount = testRunner.CountTestCases(new NameFilter());

                var testToRun = GetMatchingTest(methodName, testRunner.Test);
                var testFilter = new NameFilter(testToRun.TestName);

                TestResult result = testRunner.Run(new NullListener(), testFilter);

                if (result.IsSuccess)
                {
                    _testResultReporter.ReportTestResult(testId, "Passed", string.Empty);
                }
                else 
                {
                    result = GetMatchingTestResult(methodName, result);
                    if (result != null)
                    {
                        if (result.ResultState == ResultState.Inconclusive)
                        {
                            _testResultReporter.ReportTestResult(testId, "Inconclusive", string.Format("{0} {1}", result.Message, result.StackTrace));
                        }
                        else
                        {
                            _testResultReporter.ReportTestResult(testId, "Failed", string.Format("{0} {1}", result.Message, result.StackTrace));
                        }
                    }
                    else
                    {
                        _testResultReporter.ReportTestResult(testId, "Failed", string.Empty);
                    }
                }

                testRunner.Unload();
            }
        }

        private TestResult GetMatchingTestResult(string methodName, TestResult result)
        {
            if (result.Name == methodName)
            {
                return result;
            }

            if (result.Results != null)
            {
                foreach (TestResult subResult in result.Results)
                {
                    var matchingSubTest = GetMatchingTestResult(methodName, subResult);
                    if (matchingSubTest != null)
                    {
                        return matchingSubTest;
                    }
                }
            }

            return null;
        }

        private ITest GetMatchingTest(string methodName, ITest currentTest)
        {
            if (currentTest.TestName.Name == methodName)
            {
                return currentTest;
            }

            if (currentTest.Tests != null)
            {
                foreach (ITest test in currentTest.Tests)
                {
                    var matchingSubTest = GetMatchingTest(methodName, test);
                    if (matchingSubTest != null)
                    {
                        return matchingSubTest;
                    }
                }
            }

            return null;
        }
    }
}