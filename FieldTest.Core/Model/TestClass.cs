using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using System.Collections.ObjectModel;

namespace FieldTest.Core.Model
{
    public class TestClass : BaseTestNavigator
    {
        public TestClass(TestAssembly assembly, string className, string classNamespace)
        {
            Assembly = assembly;
            ClassName = className;
            Namespace = classNamespace;

            Tests = new ObservableCollection<TestDetails>();
        }

        public ObservableCollection<TestDetails> Tests { get; set; }

        public string ClassName { get; set; }

        public string Namespace { get; set; }

        public TestAssembly Assembly { get; protected set; }

        public override string ToString()
        {
            return string.Format("{0}.{1}", Namespace, ClassName);
        }

        public override bool IsSelected
        {
            get
            {
                return base.IsSelected;
            }
            set
            {
                if (Tests != null)
                {
                    foreach (var test in Tests)
                    {
                        test.IsSelected = value;
                    }
                }

                base.IsSelected = value;
            }
        }
    }
}