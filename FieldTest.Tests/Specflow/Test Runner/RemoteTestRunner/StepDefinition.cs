using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TechTalk.SpecFlow;
using FieldTest.Core.Model;
using Moq;
using FieldTest.Core;
using System.IO;
using System.Reflection;

namespace FieldTest.Tests.Specflow.Test_Runner.RemoteTestRunner
{
    [Binding]
    public class StepDefinition
    {
        private Core.RemoteTestRunner _remoteTestRunner;
        private List<TestDetails> _tests;
        private Mock<IConfigurationManager> _configMock;
        private Mock<ITestExecutor> _executorMock;

        [Given(@"I have a test assembly with a valid configuration file")]
        public void GivenIHaveATestAssemblyWithAValidConfigurationFile()
        {
            _executorMock = new Mock<ITestExecutor>();
            _executorMock.Setup(m => m.RunTests(It.IsAny<IEnumerable<TestDetails>>(), It.IsAny<TestAssembly>(), It.IsAny<string>(), null));

            _configMock = new Mock<IConfigurationManager>();
            string path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\FieldTest.Runner.exe.config";
            _configMock.Setup(m => m.CopyApplicationConfiguration(It.IsAny<string>(), It.IsAny<string>()));

            _remoteTestRunner = new Core.RemoteTestRunner(null, _configMock.Object, _executorMock.Object);

            var testAssembly = new TestAssembly("", "ValidAssembly", "This is a string to a valid location");
            var testClass = new TestClass(testAssembly, "ValidClass", "Valid.Namespace");
            var test = new TestDetails(testClass, "TestMethod", "", 0);

            _tests = new List<TestDetails>()
            {
                test
            };
        }

        [Given(@"no other test assemblies in my solution")]
        public void GivenNoOtherTestAssembliesInMySolution()
        {
            // Placeholder, do nothing
        }

        [When(@"I run the tests for that configuration")]
        public void WhenIRunTheTestsForThatConfiguration()
        {
            _remoteTestRunner.RunTests(_tests);
        }

        [Then(@"the application configuration file should be copied to the remote test runner's file location")]
        public void ThenTheApplicationConfigurationFileShouldBeCopiedToTheRemoteTestRunnerSFileLocation()
        {
            _configMock.VerifyAll();
        }
    }
}