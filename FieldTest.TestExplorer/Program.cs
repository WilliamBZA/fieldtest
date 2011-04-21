using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FieldTest.TestExplorer
{
    class Program
    {
        static void Main(string[] args)
        {
            var mergedParameters = string.Join(string.Empty, args);

            var individualArguments = mergedParameters.Split(new string[] { "=>" }, StringSplitOptions.RemoveEmptyEntries);

            ProcessAssemblies(individualArguments);
        }

        private static void ProcessAssemblies(string[] individualArguments)
        {
            foreach (var argument in individualArguments)
            {
                try
                {
                    var assemblyPath = ExtractValue("ASSEMBLYPATH:{", argument).Replace("#", " ");

                    Assembly assembly = Assembly.LoadFrom(assemblyPath);

                    CheckAssemblyForTests(assembly);
                }
                catch { }
            }
        }

        private static string ExtractValue(string tokenPrefix, string result)
        {
            var valueAfterToken = result.Split(new string[] { tokenPrefix }, StringSplitOptions.None)[1];
            var extractedResult = valueAfterToken.Substring(0, valueAfterToken.IndexOf("};"));

            return extractedResult;
        }

        private static void CheckAssemblyForTests(Assembly assembly)
        {
            if (assembly != null)
            {
                var types = assembly.GetTypes();

                foreach (var type in types)
                {
                    CheckTypesForTests(type);
                }
            }
        }

        private static void CheckTypesForTests(Type type)
        {
            var methods = type.GetMethods();

            var frameworkTypes = new Type[] { typeof(TestMethodAttribute), typeof(NUnit.Framework.TestAttribute) };

            foreach (var method in methods)
            {
                var attributes = method.GetCustomAttributes(true);

                foreach (var attribute in attributes)
                {
                    var attributeType = attribute.GetType();

                    if (frameworkTypes.Contains(attributeType))
                    {
                        var testType = "NUnit";

                        if (attributeType == typeof(TestMethodAttribute))
                        {
                            testType = "MsTest";
                        }

                        Console.WriteLine("ASSEMBLYPATH:{{{0}}};ASSEMBLYNAME:{{{1}}};FRAMEWORKTYPE:{{{2}}};CLASSNAME:{{{3}}};CLASSNAMESPACE:{{{4}}};METHOD:{{{5}}};", type.Assembly.Location, type.Assembly.GetName().Name, testType, type.Name, type.Namespace, method.Name);
                    }
                }
            }
        }
    }
}
