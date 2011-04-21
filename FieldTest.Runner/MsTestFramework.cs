using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace FieldTest.Runner
{
    public class MsTestFramework : BaseTestFramework
    {
        private TestContext _context;

        public MsTestFramework(ITestResultReporter testResultReporter)
            : base(testResultReporter)
        {
            _context = GetTestContext();
        }

        public override void InitializeClass(object classInstance, MethodInfo[] _classMethods)
        {
            InvokeInstanceMethods(classInstance, _classMethods, typeof(ClassInitializeAttribute), new object[] { _context });
        }

        public override void CleanupClass(object classInstance, MethodInfo[] _classMethods)
        {
            InvokeInstanceMethods(classInstance, _classMethods, typeof(ClassCleanupAttribute));
        }

        public override void InitializeTest(object classInstance, MethodInfo[] _classMethods)
        {
            InvokeInstanceMethods(classInstance, _classMethods, typeof(TestInitializeAttribute));
        }

        public override void CleanupTest(object classInstance, MethodInfo[] _classMethods)
        {
            InvokeInstanceMethods(classInstance, _classMethods, typeof(TestCleanupAttribute));
        }

        private TestContext GetTestContext()
        {
            Mock<TestContext> testContext = new Mock<TestContext>();
            return testContext.Object;
        }

        public override void ExecuteTest(object instance, string testId, string methodName)
        {
            try
            {
                var methods = instance.GetType().GetMethods();

                // Init Tests
                InitializeTest(instance, methods);

                // Invoke test method
                InvokeTest(instance, methods, testId, methodName);

                // Cleanup Tests
                CleanupTest(instance, instance.GetType().GetMethods());

                _testResultReporter.ReportTestResult(testId, "Passed", string.Empty);
            }
            catch (AssertInconclusiveException ex)
            {
                _testResultReporter.ReportTestResult(testId, "Inconclusive", ex.InnerException.ToString());
            }
            catch (TargetInvocationException ex)
            {
                if (ex.InnerException is AssertInconclusiveException)
                {
                    _testResultReporter.ReportTestResult(testId, "Inconclusive", ex.InnerException.ToString());
                }
                else
                {
                    _testResultReporter.ReportTestResult(testId, "Failed", ex.InnerException.ToString());
                }
            }
            catch (Exception ex)
            {
                _testResultReporter.ReportTestResult(testId, "Failed", ex.ToString());
            }
        }
    }
}