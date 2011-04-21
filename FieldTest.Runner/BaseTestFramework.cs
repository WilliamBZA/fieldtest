using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace FieldTest.Runner
{
    public abstract class BaseTestFramework
    {
        protected ITestResultReporter _testResultReporter;
        
        public BaseTestFramework(ITestResultReporter testResultReporter)
        {
            _testResultReporter = testResultReporter;
        }

        public abstract void InitializeClass(object classInstance, MethodInfo[] _classMethods);
        public abstract void CleanupClass(object classInstance, MethodInfo[] _classMethods);

        public abstract void InitializeTest(object classInstance, MethodInfo[] _classMethods);
        public abstract void CleanupTest(object classInstance, MethodInfo[] _classMethods);

        public abstract void ExecuteTest(object instance, string testId, string methodName);

        protected virtual void InvokeInstanceMethods(object instance, MethodInfo[] objectMethods, Type type)
        {
            InvokeInstanceMethods(instance, objectMethods, type, null);
        }

        protected virtual void InvokeTest(object _testInstance, MethodInfo[] methods, string testId, string testName)
        {
            var testMethod = (from method
                              in methods
                              where method.Name == testName
                              select method).FirstOrDefault();

            testMethod.Invoke(_testInstance, new object[] { });
        }

        protected virtual void InvokeInstanceMethods(object classInstance, MethodInfo[] objectMethods, Type type, object[] parameters)
        {
            var methodsWithAttributes = objectMethods.Where(m => m.GetCustomAttributes(type, true).Length > 0);

            foreach (var initializer in methodsWithAttributes)
            {
                object[] paramList = parameters ?? new object[] { };

                initializer.Invoke(classInstance, paramList);
            }
        }
    }
}
