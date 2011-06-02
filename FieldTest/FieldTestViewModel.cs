using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FieldTest.Core.Model;
using System.ComponentModel;
using Microsoft.VisualStudio.Shell.Interop;
using System.Reflection;
using EnvDTE;
using FieldTest.Core;
using System.Collections.ObjectModel;
using VSLangProj;

namespace FieldTest
{
    public class FieldTestViewModel : INotifyPropertyChanged
    {
        public FieldTestViewModel()
        {
            TestsAreRunning = false;
            TestAssemblies = new ObservableCollection<TestAssembly>();
        }

        private ObservableCollection<TestAssembly> _testAssemblies;
        public ObservableCollection<TestAssembly> TestAssemblies
        {
            get { return _testAssemblies; }

            set
            {
                if (_testAssemblies != value)
                {
                    _testAssemblies = value;

                    if (PropertyChanged != null)
                    {
                        PropertyChanged(this, new PropertyChangedEventArgs("TestAssemblies"));
                    }
                }
            }
        }

        private bool _testsAreRunning;
        public bool TestsAreRunning
        {
            get { return _testsAreRunning; }

            set
            {
                if (_testsAreRunning != value)
                {
                    _testsAreRunning = value;

                    if (PropertyChanged != null)
                    {
                        PropertyChanged(this, new PropertyChangedEventArgs("TestsAreRunning"));
                    }
                }
            }
        }

        private bool _canRunTests;
        public bool CanRunTests
        {
            get { return _canRunTests; }

            set
            {
                if (_canRunTests != value)
                {
                    _canRunTests = value;

                    if (PropertyChanged != null)
                    {
                        PropertyChanged(this, new PropertyChangedEventArgs("CanRunTests"));
                    }
                }
            }
        }

        public void LoadTests(IVsSolution solution)
        {
            var newAssemblies = EnumerateAllProjects(solution);

            CheckAssembliesForMissingTests(newAssemblies);
            CheckNoOrphanAssembliesExist(newAssemblies);

            CanRunTests = true;
        }

        private void CheckNoOrphanAssembliesExist(List<TestAssembly> newAssemblies)
        {
            var assembliesToRemove = new List<TestAssembly>();

            foreach (var assembly in TestAssemblies)
            {
                var newAssembly = newAssemblies.FirstOrDefault(a => a.AssemblyPath == assembly.AssemblyPath);

                if (newAssembly == null)
                {
                    assembliesToRemove.Add(assembly);
                }
                else
                {
                    CheckNoOrphanClassesExist(assembly, newAssembly);
                }
            }

            foreach (var assembly in assembliesToRemove)
            {
                TestAssemblies.Remove(assembly);
            }
        }

        private static void CheckNoOrphanClassesExist(TestAssembly assembly, TestAssembly newAssembly)
        {
            var classesToRemove = new List<TestClass>();

            foreach (var oldClass in assembly.TestClasses)
            {
                var newClass = newAssembly.TestClasses.FirstOrDefault(fc => string.Format("{0}.{1}", fc.Namespace, fc.ClassName) == string.Format("{0}.{1}", oldClass.Namespace, oldClass.ClassName));

                if (newClass == null)
                {
                    classesToRemove.Add(oldClass);
                }
                else
                {
                    CheckNoOrhpanTestsExist(oldClass, newClass);
                }
            }

            foreach (var oldClass in classesToRemove)
            {
                assembly.TestClasses.Remove(oldClass);
            }
        }

        private static void CheckNoOrhpanTestsExist(TestClass oldClass, TestClass newClass)
        {
            var testsToRemove = new List<TestDetails>();

            foreach (var oldTest in oldClass.Tests)
            {
                if (oldTest != null)
                {
                    var newTest = newClass.Tests.FirstOrDefault(t => t.MethodName == oldTest.MethodName);

                    if (newTest == null)
                    {
                        testsToRemove.Add(oldTest);
                    }
                }
            }

            foreach (var test in testsToRemove)
            {
                oldClass.Tests.Remove(test);
            }
        }

        private void CheckAssembliesForMissingTests(List<TestAssembly> newAssemblies)
        {
            foreach (var assembly in newAssemblies)
            {
                var foundAssembly = TestAssemblies.FirstOrDefault(ta => ta.AssemblyPath == assembly.AssemblyPath);

                if (foundAssembly == null)
                {
                    TestAssemblies.Add(assembly);
                }
                else
                {
                    CheckClassesForMissingTests(assembly, foundAssembly);
                }
            }
        }

        private static void CheckClassesForMissingTests(TestAssembly assembly, TestAssembly foundAssembly)
        {
            foreach (var testClass in assembly.TestClasses)
            {
                var foundClass = foundAssembly.TestClasses.FirstOrDefault(fc => string.Format("{0}.{1}", fc.Namespace, fc.ClassName) == string.Format("{0}.{1}", testClass.Namespace, testClass.ClassName));

                if (foundClass == null)
                {
                    foundAssembly.TestClasses.Add(testClass);
                }
                else
                {
                    CheckTestMethodsForMissingTests(testClass, foundClass);
                }
            }
        }

        private static void CheckTestMethodsForMissingTests(TestClass testClass, TestClass foundClass)
        {
            foreach (var test in testClass.Tests)
            {
                var foundTest = foundClass.Tests.FirstOrDefault(t => t.MethodName == test.MethodName);
                if (foundTest == null)
                {
                    foundClass.Tests.Add(test);
                }
            }
        }

        internal static TestStatus SetStatusToPreviouslyRun(TestStatus testStatus)
        {
            switch (testStatus)
            {
                case TestStatus.NotRun:
                    return TestStatus.NotRunPrevious;

                case TestStatus.Changed:
                    return TestStatus.ChangedPrevious;

                case TestStatus.Passed:
                    return TestStatus.PassedPrevious;

                case TestStatus.Failed:
                    return TestStatus.FailedPrevious;

                default:
                    return testStatus;
            }
        }

        private List<TestAssembly> EnumerateAllProjects(IVsSolution solution)
        {
            var projects = GetProjectsFromSolution(solution, string.Empty);

            var assemblies = (from project
                                 in projects
                              select GetProjectOutputDirectory(project)).ToList();

            ITestFinder testFinder = new CCITestFinder(); //new RemoteTestFinder();

            return testFinder.FindTestsInProjects(assemblies);
        }

        public static Project[] GetProjectsFromSolution(IVsSolution solution, string projectKind)
        {
            var projects = new List<Project>();

            if (solution == null)
            {
                return projects.ToArray();
            }

            IEnumHierarchies ppEnum;
            var tempGuid = Guid.Empty;
            solution.GetProjectEnum((uint)Microsoft.VisualStudio.Shell.Interop.__VSENUMPROJFLAGS.EPF_ALLPROJECTS, ref tempGuid, out ppEnum);

            if (ppEnum != null)
            {
                uint actualResult = 0;
                var nodes = new IVsHierarchy[1];
                while (0 == ppEnum.Next(1, nodes, out actualResult))
                {
                    Object obj;
                    nodes[0].GetProperty((uint)Microsoft.VisualStudio.VSConstants.VSITEMID_ROOT, (int)Microsoft.VisualStudio.Shell.Interop.__VSHPROPID.VSHPROPID_ExtObject, out obj);

                    var project = obj as Project;
                    if (project != null)
                    {
                        if (string.IsNullOrEmpty(projectKind))
                        {
                            projects.Add(project);
                        }
                        else if (projectKind.Equals(project.Kind, StringComparison.InvariantCultureIgnoreCase))
                        {
                            projects.Add(project);
                        }
                    }
                }
            }

            return projects.ToArray();
        }

        private ProjectDirectoryWrapper GetProjectOutputDirectory(Project project)
        {
            if (project.ConfigurationManager != null)
            {
                ProjectDirectoryWrapper output = new ProjectDirectoryWrapper();

                try
                {
                    Microsoft.Build.Evaluation.Project msproject = new Microsoft.Build.Evaluation.Project(project.FileName);

                    if (msproject != null)
                    {
                        List<string> hintPaths = new List<string>();

                        foreach (Microsoft.Build.Evaluation.ProjectItem projectItem in msproject.Items)
                        {
                            var hintPath = projectItem.GetMetadata("HintPath");
                            if (hintPath != null)
                            {
                                var path = System.IO.Path.GetDirectoryName(System.IO.Path.GetFullPath(hintPath.EvaluatedValue));
                                if (!hintPaths.Contains(path))
                                {
                                    hintPaths.Add(path);
                                }
                            }
                        }

                        output.HintPaths = hintPaths;
                    }
                }
                catch { }

                var outputDirProperty = project.ConfigurationManager.ActiveConfiguration.Properties.Item("OutputPath");
                var assemblyNameProperty = project.Properties.Item("OutputFileName");

                if (assemblyNameProperty != null)
                {
                    var assemblyName = assemblyNameProperty.Value.ToString();

                    if (outputDirProperty != null)
                    {
                        output.OutputDirectory = string.Format("{0}\\{1}\\{2}", System.IO.Path.GetDirectoryName(project.FullName), outputDirProperty.Value, assemblyName);

                        return output;
                    }
                }
            }

            return null;
        }

        private Assembly TryLoadAssembly(string solutionPath, string projectPath, string outputDir, string configurationName, string assemblyName)
        {
            try
            {
                var assembly = Assembly.LoadFrom(string.Format(projectPath + "\\{0}\\{2}", outputDir, configurationName, assemblyName));
                return assembly;
            }
            catch { }

            return null;
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}