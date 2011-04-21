using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace FieldTest.Runner
{
    public class ConsoleTestResultReporter : ITestResultReporter
    {
        private TextWriter _consoleOut;

        public ConsoleTestResultReporter()
        {
            _consoleOut = Console.Out;
        }

        public void ReportTestResult(string testId, string result, string message)
        {
            _consoleOut.WriteLine("TESTID:{{{0}}};STATUS:{{{1}}};MESSAGE:{{{2}}};", testId, result, message);
        }
    }
}