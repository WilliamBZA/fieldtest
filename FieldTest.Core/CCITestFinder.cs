using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FieldTest.Core.Model;
using Microsoft.Cci;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NUnit.Framework;

namespace FieldTest.Core
{
    public class CCITestFinder : ITestFinder
    {
        public List<TestAssembly> FindTestsInProjects(List<ProjectDirectoryWrapper> assemblies)
        {
            List<TestWrapper> tests = new List<TestWrapper>();

            using (var host = new PeReader.DefaultHost())
            {
                foreach (var pathWrapper in assemblies)
                {
                    if (pathWrapper != null && !string.IsNullOrEmpty(pathWrapper.OutputDirectory))
                    {
                        var module = host.LoadUnitFrom(pathWrapper.OutputDirectory.Replace(" ", "#")) as IModule;

                        foreach (var type in module.GetAllTypes())
                        {
                            foreach (var method in type.Methods)
                            {
                                if (method.Attributes.Any(attribute => attribute.Type.ToString() == typeof(TestAttribute).ToString() || attribute.Type.ToString() == typeof(TestMethodAttribute).ToString()))
                                {
                                    foreach (var location in method.Locations)
                                    {
                                        PdbReader pdbReader = null;
                                        string pdbFile = Path.ChangeExtension(module.Location, "pdb");
                                        if (File.Exists(pdbFile))
                                        {
                                            Stream pdbStream = File.OpenRead(pdbFile);
                                            pdbReader = new PdbReader(pdbStream, host);

                                            using (pdbReader)
                                            {
                                                var sourceLocations = pdbReader.GetPrimarySourceLocationsFor(location);

                                                foreach (var sourceLocation in sourceLocations)
                                                {
                                                    tests.Add(new TestWrapper()
                                                        {
                                                            AssemblyPath = module.Location,
                                                            AssemblyName = module.Name.Value,
                                                            ClassName = type.Name.Value,
                                                            ClassNamespace = type.ToString().Substring(0, type.ToString().Length - type.Name.Value.Length - 1),
                                                            LineNumber = sourceLocation.StartLine,
                                                            Filename = sourceLocation.SourceDocument.Location,
                                                            MethodName = method.Name.Value
                                                        });
                                                    // Yay!!
                                                }
                                            }
                                        }
                                        else
                                        {
                                            // Just add the class, no line numbers
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return ExtractAssembliesFromWrapper(tests);
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
                                            Type = details.FrameworkType,
                                            LineNumber = details.LineNumber,
                                            Filename = details.Filename
                                        }).Distinct();

                    foreach (var test in testsInClass)
                    {
                        var newTest = new TestDetails(newClass, test.MethodName, test.Filename, test.LineNumber);
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
            internal string Filename { get; set; }
            internal int LineNumber { get; set; }
        }
    }
}