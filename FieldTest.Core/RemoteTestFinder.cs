using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Reflection;
using FieldTest.Core.Model;
using System.Diagnostics;

namespace FieldTest.Core
{
    public class RemoteTestFinder
    {
        public RemoteTestFinder()
        {
        }

        public List<TestAssembly> FindTestsInProjects(List<ProjectDirectoryWrapper> assemblies)
        {
            var testArguments = new StringBuilder();

            foreach (var pathWrapper in assemblies)
            {
                if (pathWrapper != null && !string.IsNullOrEmpty(pathWrapper.OutputDirectory))
                {
                    /*
                                            ASSEMBLYPATH:{Path};=>ASSEMBLYPATH:{Path};
                                    */
                    if (testArguments.Length > 0)
                    {
                        testArguments.Append("=>");
                    }

                    testArguments.Append("ASSEMBLYPATH:{");
                    testArguments.Append(pathWrapper.OutputDirectory.Replace(" ", "#"));
                    testArguments.Append("};");
                }
            }

            ProcessStartInfo testExplorerStartParameters = new ProcessStartInfo()
            {
                FileName = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\FieldTest.TestExplorer.exe",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                Arguments = testArguments.ToString()
            };

            using (var testExplorerProcess = new Process())
            {
                testExplorerProcess.StartInfo = testExplorerStartParameters;
                testExplorerProcess.Start();

                var runnerResult = testExplorerProcess.StandardOutput.ReadToEnd();

                if (!string.IsNullOrEmpty(runnerResult))
                {
                    var testResults = runnerResult.Split(new string[] { "\n" }, StringSplitOptions.RemoveEmptyEntries);

                    var testWrappers = ParseTestsFromResults(testResults);

                    var tests = ExtractAssembliesFromWrapper(testWrappers);

                    return tests;
                }
            }

            return new List<TestAssembly>();
        }

        private List<TestWrapper> ParseTestsFromResults(string[] testResults)
        {
            List<TestWrapper> results = new List<TestWrapper>();

            // ASSEMBLYPATH:{Path};ASSEMBLYNAME:{Name};FRAMEWORKTYPE:{Namespace.Type};CLASSNAME:{ClassName};CLASSNAMESPACE:{Namespace};METHOD:{MethodName};
            foreach (var result in testResults)
            {
                var assemblyPath = ExtractValue("ASSEMBLYPATH:{", result);
                var assemblyName = ExtractValue("ASSEMBLYNAME:{", result);
                var frameworkTypeString = ExtractValue("FRAMEWORKTYPE:{", result);
                var className = ExtractValue("CLASSNAME:{", result);
                var classNamespace = ExtractValue("CLASSNAMESPACE:{", result);
                var testMethod = ExtractValue("METHOD:{", result);

                results.Add(new TestWrapper()
                    {
                        AssemblyName = assemblyName,
                        AssemblyPath = assemblyPath,
                        ClassName = className,
                        ClassNamespace = classNamespace,
                        MethodName = testMethod,
                        FrameworkType = LoadType(frameworkTypeString)
                    });
            }

            return results;
        }

        private TestFramework LoadType(string frameworkTypeString)
        {
            return (TestFramework)Enum.Parse(typeof(TestFramework), frameworkTypeString);
        }

        private string ExtractValue(string tokenPrefix, string result)
        {
            var valueAfterToken = result.Split(new string[] { tokenPrefix }, StringSplitOptions.None)[1];
            var extractedResult = valueAfterToken.Substring(0, valueAfterToken.IndexOf("};"));

            return extractedResult;
        }

        private List<TestAssembly> ExtractAssembliesFromWrapper(IEnumerable<TestWrapper> wrapper)
        {
            List<TestAssembly> testAssemblies = new List<TestAssembly>();

            var assemblies = (from details
                                  in wrapper
                              select new
                              {
                                  Name = details.AssemblyName,
                                  Path = details.AssemblyPath
                              }).Distinct();

            TestAssembly newAssembly;
            foreach (var assembly in assemblies)
            {
                string assemblyName = assembly.Name;
                string assemblyPath = assembly.Path;

                newAssembly = new TestAssembly(string.Empty, assemblyName, assemblyPath);

                // Get classes
                var classesInAssembly = (from details
                                             in wrapper
                                         where
                                             details.AssemblyPath == assemblyPath &&
                                             details.AssemblyName == assemblyName
                                         select new
                                                    {
                                                        ClassName = details.ClassName,
                                                        Namespace = details.ClassNamespace
                                                    }).Distinct();

                foreach (var testClass in classesInAssembly)
                {
                    string className = testClass.ClassName;
                    string classNamespace = testClass.Namespace;

                    var newClass = new TestClass(newAssembly, testClass.ClassName, testClass.Namespace);

                    // Get tests for class
                    var testsInClass = (from details
                                            in wrapper
                                        where
                                            details.AssemblyPath == assemblyPath &&
                                            details.AssemblyName == assemblyName &&
                                            details.ClassName == className &&
                                            details.ClassNamespace == classNamespace
                                        select new
                                        {
                                            MethodName = details.MethodName,
                                            Type = details.FrameworkType
                                        }).Distinct();

                    foreach (var test in testsInClass)
                    {
                        var newTest = new TestDetails(newClass, test.MethodName);
                        newTest.TestFramework = test.Type;

                        newClass.Tests.Add(newTest);
                    }

                    newAssembly.TestClasses.Add(newClass);
                }

                testAssemblies.Add(newAssembly);
            }

            return testAssemblies;
        }

        internal class TestWrapper
        {
            internal string AssemblyName { get; set; }
            internal string AssemblyPath { get; set; }
            internal TestFramework FrameworkType { get; set; }
            internal string MethodName { get; set; }
            internal string ClassName { get; set; }
            internal string ClassNamespace { get; set; }
        }
    }
}