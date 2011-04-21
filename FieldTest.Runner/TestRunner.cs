using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using FieldTest.Core.Model;
using System.IO;
using System.Globalization;

namespace FieldTest.Runner
{
    [Serializable]
    public class TestRunner
    {
        public Dictionary<string, string> TestsToRun { get; internal set; }

        private Assembly _assembly;
        private object _testInstance;
        private MethodInfo[] _classMethods;
        private BaseTestFramework _testFramework;
        private List<string> _hintPaths;

        public TestRunner(BaseTestFramework testFramework)
        {
            _testFramework = testFramework;

            AppDomain currentDomain = AppDomain.CurrentDomain;
            currentDomain.AssemblyResolve += new ResolveEventHandler(currentDomain_AssemblyResolve);

            TestsToRun = new Dictionary<string, string>();
        }

        internal void SetAssembly(string assemblyPath, List<string> assemblyHintPaths)
        {
            _assembly = Assembly.LoadFrom(assemblyPath);
            _hintPaths = assemblyHintPaths;
        }

        public Assembly currentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            string assemblyName = args.Name.Split(',')[0];
            if (_hintPaths != null && _hintPaths.Count > 0)
            {
                foreach (string extension in new string[] { "dll", "exe" })
                {
                    foreach (var hint in _hintPaths)
                    {
                        string potentialFile = Path.Combine(Path.GetDirectoryName(hint), string.Format(CultureInfo.CurrentCulture, "{0}.{1}", assemblyName, extension));
                        if (File.Exists(potentialFile))
                        {
                            return Assembly.LoadFrom(potentialFile);
                        }
                    }
                }
            }

            return null;
        }

        internal void SetClass(string className)
        {
            _testInstance = _assembly.CreateInstance(className);

            _classMethods = _testInstance.GetType().GetMethods();
        }

        private void InitializeInstance(object classInstance)
        {
            _testFramework.InitializeClass(classInstance, _classMethods);
        }

        private void CleanupInstance(object classInstance)
        {
            _testFramework.CleanupClass(classInstance, _classMethods);
        }

        private void InitializeTest(object classInstance)
        {
            _testFramework.InitializeTest(classInstance, _classMethods);
        }

        private void CleanupTest(object classInstance)
        {
            _testFramework.CleanupTest(classInstance, _classMethods);
        }

        internal void AddTest(string testId, string methodName)
        {
            TestsToRun.Add(testId, methodName);
        }

        internal void RunTestSuite()
        {
            InitializeInstance(_testInstance);

            foreach (var test in TestsToRun)
            {
                _testFramework.ExecuteTest(_testInstance, test.Key, test.Value);
            }

            CleanupInstance(_testInstance);
        }
    }
}