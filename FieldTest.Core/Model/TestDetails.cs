using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace FieldTest.Core.Model
{
    public class TestDetails : BaseTestNavigator
    {
        public TestDetails(TestClass testClass, string methodName)
            : this(testClass, methodName, null, null)
        {
        }

        public TestDetails(TestClass testClass, string methodName, string filename, int? lineNumber)
        {
            Class = testClass;
            MethodName = methodName;
            Filename = filename;
            LineNumber = lineNumber;
        }

        public string MethodName { get; set; }

        public string Filename { get; set; }

        public int? LineNumber { get; set; }

        public string Message { get; set; }

        public TestFramework TestFramework { get; set; }

        public TestClass Class { get; private set; }

        public string GetFilenameFromMessage()
        {
            if (Message.Contains(":line"))
            {
                string lineSection = Message.Substring(0, Message.IndexOf(":line "));

                if (lineSection.Contains(" in "))
                {
                    lineSection = lineSection.Substring(lineSection.IndexOf(" in ") + " in ".Length);

                    return lineSection;
                }
            }

            return string.Empty;
        }

        public int GetLineNumberFromMessage()
        {
            if (Message.Contains(":line "))
            {
                string lineSection = Message.Substring(Message.IndexOf(":line ") + ":line ".Length);
                if (lineSection.Contains(" "))
                {
                    lineSection = lineSection.Substring(0, lineSection.IndexOf(" "));
                }

                try
                {
                    return Int32.Parse(lineSection);
                }
                catch { }
            }

            return -1;
        }

        public override string ToString()
        {
            return MethodName;
        }

        public string ToFullString()
        {
            return string.Format("{0}: {1}.{2}.{3}", Class.Assembly.AssemblyName, Class.Namespace, Class.ClassName, ToString());
        }
    }
}