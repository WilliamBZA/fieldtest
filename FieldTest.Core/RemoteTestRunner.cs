using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using FieldTest.Core.Model;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Diagnostics;
using System.ServiceModel;
using System.Configuration;
using System.Threading;

namespace FieldTest.Core
{
    public class RemoteTestRunner : IConfigurationManager
    {
        private IOutputWriter _outputWriter;
        private IConfigurationManager _configManager;
        private ITestExecutor _testExecutor;

        public RemoteTestRunner(IOutputWriter outputWriter)
            : this(outputWriter, null, null)
        {
        }

        public RemoteTestRunner(IOutputWriter outputWriter, IConfigurationManager configManager, ITestExecutor testExecutor)
        {
            _outputWriter = outputWriter;
            _configManager = configManager ?? this;
            _testExecutor = testExecutor ?? new ConsoleApplicationTestExecutor(_outputWriter);
        }

        protected virtual void RunTests(IEnumerable<TestDetails> tests, IDebugAttacher debuggerAttacher)
        {
            if (_outputWriter != null)
            {
                _outputWriter.ClearOutput();
            }

            var testAssemblies = tests.Select(t => t.Class.Assembly).Distinct();

            foreach (var assembly in testAssemblies)
            {
                var runnerPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\FieldTest.Runner.exe";
                _configManager.CopyApplicationConfiguration(string.Format("{0}.config", assembly.AssemblyPath), string.Format("{0}.config", runnerPath));

                _testExecutor.RunTests(tests, assembly, runnerPath, debuggerAttacher);
            }
        }

        public void CopyApplicationConfiguration(string oldConfigFilePath, string newConfigFilePath)
        {
            if (System.IO.File.Exists(oldConfigFilePath))
            {
                var oldConfig = ConfigurationManager.OpenExeConfiguration(oldConfigFilePath);

                oldConfig.SaveAs(newConfigFilePath);
            }
        }



        public void RunTests(IEnumerable<TestDetails> tests)
        {
            RunTests(tests, null);
        }

        public void DebugTests(IEnumerable<TestDetails> tests, IDebugAttacher debuggerAttacher)
        {
            RunTests(tests, debuggerAttacher);
        }
    }
}