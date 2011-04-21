using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EnvDTE;
using FieldTest.Core;

namespace FieldTest
{
    public class OutputWindowWriter : IOutputWriter
    {
        private DTE _dteReference;
        private OutputWindowPane _outputPane;
        
        public OutputWindowWriter(DTE dteReference)
        {
            _dteReference = dteReference;
        }

        public OutputWindowPane OutputPane
        {
            get
            {
                if (_outputPane == null)
                {
                    var outputWindow = (OutputWindow)_dteReference.Windows.Item(EnvDTE.Constants.vsWindowKindOutput).Object;

                    foreach (OutputWindowPane pane in outputWindow.OutputWindowPanes)
                    {
                        if (pane.Name == "Field Test")
                        {
                            _outputPane = pane;
                            break;
                        }
                    }

                    if (_outputPane == null)
                    {
                        _outputPane = outputWindow.OutputWindowPanes.Add("Field Test");
                    }
                }

                return _outputPane;
            }
        }

        public void Write(string message)
        {
            OutputPane.Activate();
            OutputPane.OutputString(string.Format("{0}\r\n", message));
        }

        public void ClearOutput()
        {
            OutputPane.Activate();
            OutputPane.Clear();
        }
    }
}