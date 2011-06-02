using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using FieldTest.Core.Model;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace FieldTest.Runner
{
    public class Program
    {
        static void Main(string[] args)
        {
            // Input params:
            // ASSEMBLY=>{AssemblyID}
            // {Path}
            // {HintPath}
            // {HintPath}
            // {HintPath}
            // ;
            // CLASS=>{FullClassName}
            // {AssemblyId}
            // ;
            // TEST=>{TestId}
            // {MethodName}
            // {FullClassName}
            // ;

            Dictionary<string, TestAssembly> assemblies = new Dictionary<string, TestAssembly>();
            Dictionary<string, TestClass> classes = new Dictionary<string, TestClass>();
            Dictionary<string, TestDetails> tests = new Dictionary<string, TestDetails>();

            string inputLine;
            StringBuilder currentSection = null;
            do
            {
                inputLine = Console.ReadLine();

                if (!string.IsNullOrEmpty(inputLine))
                {
                    // We've reached the end of an input section, process the input
                    if (inputLine == ";")
                    {
                        if (currentSection != null)
                        {
                            try
                            {
                                ProcessInput(currentSection.ToString(), assemblies, classes, tests);
                            }
                            catch { }

                            currentSection = null;
                        }
                    }
                    else
                    {
                        // Still continuing input or it's a new section
                        if (currentSection == null)
                        {
                            currentSection = new StringBuilder();
                        }

                        currentSection.AppendLine(inputLine);
                    }
                }
            } while (!string.IsNullOrEmpty(inputLine));

            Console.WriteLine("Running all tests");

            try
            {
                RunAllTests(assemblies, classes, tests);
            }
            catch (Exception ex) { Console.WriteLine(ex.ToString()); }

            Console.WriteLine("Done running tests");
        }

        private static void ProcessInput(string inputChunk, Dictionary<string, TestAssembly> assemblies, Dictionary<string, TestClass> classes, Dictionary<string, TestDetails> tests)
        {
            var inputLines = inputChunk.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);

            if (inputLines.Length > 0)
            {
                if (inputLines[0].StartsWith("ASSEMBLY=>"))
                {
                    ProcessAssemblyInput(inputLines, assemblies);
                }
                else if (inputLines[0].StartsWith("CLASS=>"))
                {
                    ProcessClassInput(inputLines, assemblies, classes);
                }
                else if (inputLines[0].StartsWith("TEST=>"))
                {
                    ProcessTestInput(inputLines, classes, tests);
                }
            }
        }

        private static void ProcessAssemblyInput(string[] inputLines, Dictionary<string, TestAssembly> assemblies)
        {
            // ASSEMBLY=>{AssemblyID}
            // {Path}
            // {HintPath}
            // {HintPath}
            // {HintPath}
            // ;
            string assemblyId = ExtractValue("ASSEMBLY=>{", inputLines[0]);

            string assemblyPath = inputLines[1];
            var hintPaths = inputLines.Skip(2).ToList();

            TestAssembly newAssembly = new TestAssembly(assemblyId, string.Empty, assemblyPath);

            if (hintPaths != null && hintPaths.Count > 0)
            {
                newAssembly.HintPaths.AddRange(hintPaths);
            }
            
            assemblies.Add(assemblyId, newAssembly);
        }

        private static void ProcessClassInput(string[] inputLines, Dictionary<string, TestAssembly> assemblies, Dictionary<string, TestClass> classes)
        {
            // CLASS=>{FullClassName}
            // {AssemblyId}
            // ;
            string fullClassName = ExtractValue("CLASS=>{", inputLines[0]);
            string assemblyId = inputLines[1];

            TestClass newClass = new TestClass(assemblies[assemblyId], fullClassName, string.Empty);
            classes.Add(fullClassName, newClass);
        }

        private static void ProcessTestInput(string[] inputLines, Dictionary<string, TestClass> classes, Dictionary<string, TestDetails> tests)
        {
            // TEST=>{TestId}
            // {MethodName}
            // {FullClassName}
            // {TestFramework}
            // ;
            string testId = ExtractValue("TEST=>{", inputLines[0]);
            string testMethodName = inputLines[1];
            string fullClassName = inputLines[2];
            string testFrameworkString = inputLines[3];

            TestDetails testDetails = new TestDetails(classes[fullClassName], testMethodName, "", 0);
            testDetails.TestFramework = (TestFramework)Enum.Parse(typeof(TestFramework), testFrameworkString);
            tests.Add(testId, testDetails);
        }

        private static void RunAllTests(Dictionary<string, TestAssembly> assemblies, Dictionary<string, TestClass> classes, Dictionary<string, TestDetails> tests)
        {
            var nunitTestFramework = new NUnitTestFramework(new ConsoleTestResultReporter());
            var msTestFramework = new MsTestFramework(new ConsoleTestResultReporter());

            foreach (var testId in tests.Keys)
            {
                var test = tests[testId];

                var methodName = test.MethodName;
                var className = test.Class.ToString();
                var assemblyPath = test.Class.Assembly.AssemblyPath;
                var assemblyHintPaths = test.Class.Assembly.HintPaths;

                BaseTestFramework framework = msTestFramework;
                if (test.TestFramework == TestFramework.NUnit)
                {
                    framework = nunitTestFramework;
                }

                TestRunner runner = new TestRunner(framework);
                runner.SetAssembly(assemblyPath, assemblyHintPaths);
                runner.SetClass(className);

                runner.AddTest(testId, methodName);

                runner.RunTestSuite();
            }
        }

        private static string ExtractValue(string tokenPrefix, string result)
        {
            var valueAfterToken = result.Split(new string[] { tokenPrefix }, StringSplitOptions.None)[1];
            var extractedResult = valueAfterToken.Substring(0, valueAfterToken.IndexOf("}"));

            return extractedResult;
        }
    }
}