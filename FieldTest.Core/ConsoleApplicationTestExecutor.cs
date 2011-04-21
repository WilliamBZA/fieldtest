using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FieldTest.Core.Model;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace FieldTest.Core
{
    public class ConsoleApplicationTestExecutor : ITestExecutor
    {
        private StringBuilder _currentResultBlock;
        private Dictionary<string, TestDetails> _testHashMap;
        private IOutputWriter _outputWriter;
        private AutoResetEvent _testWaitHandle;

        public ConsoleApplicationTestExecutor(IOutputWriter outputWriter)
        {
            _outputWriter = outputWriter;
        }

        public void RunTests(IEnumerable<TestDetails> tests, TestAssembly assembly, string runnerPath, IDebugAttacher debuggerAttacher)
        {
            Dictionary<string, TestDetails> testHashTable = new Dictionary<string, TestDetails>();

            ProcessStartInfo testRunnerStartParameters = new ProcessStartInfo()
            {
                FileName = runnerPath,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            };

            using (var testRunnerProcess = new Process())
            {
                testRunnerProcess.StartInfo = testRunnerStartParameters;
                testRunnerProcess.OutputDataReceived += new DataReceivedEventHandler(testRunnerProcess_OutputDataReceived);
                testRunnerProcess.Start();

                if (debuggerAttacher != null)
                {
                    debuggerAttacher.AttachToProcess(testRunnerProcess.Id);
                }

                testRunnerProcess.BeginOutputReadLine();

                ProcessTests(assembly, tests, testRunnerProcess.StandardInput);

                //var runnerResult = testRunnerProcess.StandardOutput.ReadToEnd();
                // ProcessTestResults("", testHashTable);

                _testWaitHandle = new AutoResetEvent(false);
                _testWaitHandle.WaitOne();
            }
        }

        private void ProcessTests(TestAssembly testAssembly, IEnumerable<TestDetails> tests, StreamWriter streamWriter)
        {
            // Input params:
            // ASSEMBLY=>{AssemblyID}
            // {Path}
            // {HintPath}
            // {HintPath}
            // {HintPath}
            // ;
            var allAssemblies = (from test
                                 in tests
                                 select test.Class.Assembly).Distinct();

            Dictionary<string, int> assemblyHashtable = new Dictionary<string, int>();
            int currentId = 1;

            assemblyHashtable.Add(testAssembly.AssemblyPath, currentId);

            streamWriter.WriteLine("ASSEMBLY=>{{{0}}}", currentId.ToString());
            streamWriter.WriteLine(testAssembly.AssemblyPath);

            if (testAssembly.HintPaths != null && testAssembly.HintPaths.Count > 0)
            {
                foreach (var hint in testAssembly.HintPaths)
                {
                    streamWriter.WriteLine(hint);
                }
            }

            streamWriter.WriteLine(";");

            currentId++;

            // CLASS=>{FullClassName}
            // {AssemblyId}
            // ;
            var allClasses = (from test
                              in tests
                              where test.Class.Assembly == testAssembly
                              select test.Class).Distinct();

            currentId = 1;
            Dictionary<string, int> classHashTable = new Dictionary<string, int>();

            foreach (var currentClass in allClasses)
            {
                classHashTable.Add(currentClass.ToString(), currentId);

                streamWriter.WriteLine("CLASS=>{{{0}}}", currentClass.ToString());
                streamWriter.WriteLine(assemblyHashtable[currentClass.Assembly.AssemblyPath].ToString());
                streamWriter.WriteLine(";");

                currentId++;
            }

            // TEST=>{TestId}
            // {MethodName}
            // {FullClassName}
            // ;
            var allTests = from test
                           in tests
                           where test.Class.Assembly == testAssembly
                           select test;

            currentId = 1;
            _testHashMap = new Dictionary<string, TestDetails>();
            foreach (var test in allTests)
            {
                _testHashMap.Add(currentId.ToString(), test);

                streamWriter.WriteLine("TEST=>{{{0}}}", currentId.ToString());
                streamWriter.WriteLine(test.MethodName);
                streamWriter.WriteLine(test.Class.ToString());
                streamWriter.WriteLine(test.TestFramework.ToString());
                streamWriter.WriteLine(";");

                currentId++;
            }

            streamWriter.WriteLine(string.Empty);
            streamWriter.WriteLine(string.Empty);
        }

        void testRunnerProcess_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                if (e.Data.StartsWith("TESTID:{"))
                {
                    _currentResultBlock = new StringBuilder();
                }

                if (_currentResultBlock != null)
                {
                    _currentResultBlock.AppendLine(e.Data);
                    var currentBit = _currentResultBlock.ToString();

                    if (currentBit.EndsWith(";\r\n") && currentBit.Contains("STATUS:{") && currentBit.Contains("MESSAGE:{"))
                    {
                        var testId = ExtractValue("TESTID:{", currentBit);
                        var status = ExtractValue("STATUS:{", currentBit);
                        var message = ExtractValue("MESSAGE:{", currentBit);

                        if (_testHashMap.ContainsKey(testId))
                        {
                            var test = _testHashMap[testId];

                            test.Status = ConvertTestStatus(status);
                            test.Message = message;

                            _testHashMap.Remove(testId);

                            if (test.Status != TestStatus.Passed && _outputWriter != null)
                            {
                                _outputWriter.Write("==========================");
                                _outputWriter.Write("Test Result");
                                _outputWriter.Write("==========================");
                                _outputWriter.Write(string.Format("{0}{1}", "Test Name :".PadRight(15), test.MethodName));
                                _outputWriter.Write(string.Format("{0}{1}", "Result    :".PadRight(15), status));
                                _outputWriter.Write(string.Format("{0}{1}", "Message   :".PadRight(15), message));
                                _outputWriter.Write("\r\n");
                            }

                            if (_testHashMap.Count == 0)
                            {
                                // Done running all tests
                                if (_testWaitHandle != null)
                                {
                                    _testWaitHandle.Set();
                                }
                            }
                        }
                    }
                }
            }
        }

        private TestStatus ConvertTestStatus(string stringStatus)
        {
            return (TestStatus)Enum.Parse(typeof(TestStatus), stringStatus);
        }

        private string ExtractValue(string tokenPrefix, string result)
        {
            var valueAfterToken = result.Split(new string[] { tokenPrefix }, StringSplitOptions.None)[1];

            if (valueAfterToken.IndexOf("};") >= 0)
            {
                valueAfterToken = valueAfterToken.Substring(0, valueAfterToken.IndexOf("};"));
            }

            return valueAfterToken;
        }
    }
}