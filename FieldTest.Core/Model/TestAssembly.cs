using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using System.Collections.ObjectModel;

namespace FieldTest.Core.Model
{
    public class TestAssembly : BaseTestNavigator
    {
        public TestAssembly(string assemblyId, string assemblyName, string assemblyPath)
        {
            AssemblyId = assemblyId;
            AssemblyName = assemblyName;
            AssemblyPath = assemblyPath;

            TestClasses = new ObservableCollection<TestClass>();
            HintPaths = new List<string>();
        }

        public ObservableCollection<TestClass> TestClasses { get; internal set; }

        public string AssemblyName { get; internal set; }

        public string AssemblyPath { get; internal set; }

        public List<string> HintPaths { get; internal set; }

        public string AssemblyId { get; internal set; }

        public override string ToString()
        {
            return AssemblyName;
        }

        public override bool IsSelected
        {
            get
            {
                return base.IsSelected;
            }
            set
            {
                if (TestClasses != null)
                {
                    foreach (var testClass in TestClasses)
                    {
                        testClass.IsSelected = value;
                    }
                }

                base.IsSelected = value;
            }
        }
    }
}