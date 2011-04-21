using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Windows;
using System.Runtime.InteropServices;
using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell;
using System.Reflection;
using FieldTest.Core;
using EnvDTE80;
using Microsoft.VisualStudio.CommandBars;
using System.Diagnostics;
using FieldTest.Core.Model;

namespace FieldTest
{
    /// <summary>
    /// This class implements the tool window exposed by this package and hosts a user control.
    ///
    /// In Visual Studio tool windows are composed of a frame (implemented by the shell) and a pane, 
    /// usually implemented by the package implementer.
    ///
    /// This class derives from the ToolWindowPane class provided from the MPF in order to use its 
    /// implementation of the IVsUIElementPane interface.
    /// </summary>
    [Guid("b71e09b7-1304-434a-9a0b-1f2b236d7264")]
    public class MyToolWindow : ToolWindowPane, IEditorNavigator, IDebugAttacher
    {
        private FieldTestControl _fieldTestControl;
        private DTE _dteReference;
        private BuildEvents _buildEvents;

        /// <summary>
        /// Standard constructor for the tool window.
        /// </summary>
        public MyToolWindow() :
            base(null)
        {
            this.Caption = Resources.ToolWindowTitle;

            this.BitmapResourceID = 301;
            this.BitmapIndex = 1;
        }

        protected override void Initialize()
        {
            base.Initialize();

            _dteReference = (EnvDTE.DTE)GetService(typeof(DTE));
            _buildEvents = _dteReference.Events.BuildEvents;

            _buildEvents.OnBuildDone += new _dispBuildEvents_OnBuildDoneEventHandler(buildEvents_OnBuildDone);

            _fieldTestControl = new FieldTestControl(this, new OutputWindowWriter(_dteReference), this);

            base.Content = _fieldTestControl;
        }

        void buildEvents_OnBuildDone(vsBuildScope scope, vsBuildAction action)
        {
            var vs = GetService(typeof(SVsSolution)) as IVsSolution;

            _fieldTestControl.LoadTestsInSolution(vs);
        }

        public bool AttachToProcess(int processId)
        {
            Debugger2 dbg = _dteReference.Debugger as Debugger2;
            Transport trans = dbg.Transports.Item("Default");
            Engine eng;

            eng = trans.Engines.Item("Managed");

            try
            {
                var processes = dbg.GetProcesses(trans, "");

                foreach (EnvDTE80.Process2 process in processes)
                {
                    try
                    {
                        int pid = process.ProcessID;
                        string name = process.Name;

                        if (process.ProcessID == processId)
                        {
                            process.Attach2(eng);

                            return true;
                        }
                    }
                    catch { }
                }
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("Invalid index."))
                {
                    return false;
                }
            }

            return false;
        }

        public void GetCommandBarNameByControlCaption(DTE dte, string controlCaption)
        {
            string guid;
            int id;
            CommandBars commandBars;
            Commands allCommands = _dteReference.Commands;

            try
            {
                CommandBar menuBarCommandBar = ((CommandBars)dte.CommandBars)["MenuBar"];
                CommandBarControl testbar = menuBarCommandBar.Controls["Test"];

                allCommands.CommandInfo(testbar, out guid, out id);


                var subControls = menuBarCommandBar.Controls;

                foreach (CommandBarControl item in subControls)
                {

                }

                // The following cast is required in VS 2005 and higher because its DTE.CommandBars returns the type Object
                // (because VS 2005 and higher uses for commandbars the type Microsoft.VisualStudio.CommandBars.CommandBars 
                // of the new Microsoft.VisualStudio.CommandBars.dll assembly while VS.NET 2002/2003 used the 
                // type Microsoft.Office.Core.CommandBars of the Office.dll assembly)

                commandBars = (CommandBars)dte.CommandBars;


                foreach (Command command in allCommands)
                {
                    //if (command.Guid == "{B85579AA-8BE0-4C4F-A850-90902B317571}")
                    if (command.Name == "Test.RunAllImpactedTests")
                    {
                        Debug.WriteLine("Command name: " + command.Name);
                    }
                }

               

                foreach (CommandBar commandBar in commandBars)
                {
                    foreach (CommandBarControl commandBarControl1 in commandBar.Controls)
                    {
                        allCommands.CommandInfo(commandBarControl1, out guid, out id);
                        
                        if (guid == "{B85579AA-8BE0-4C4F-A850-90902B317571}")

                        //if (commandBarControl1.Caption == controlCaption)
                        {
                            Debug.WriteLine("----------------------------------------");
                            Debug.WriteLine("Candidate CommandBar Name: " + "\"" + commandBar.Name + "\"");
                            Debug.WriteLine("Captions on this command bar:");

                            foreach (CommandBarControl commandBarControl2 in commandBar.Controls)
                            {
                                Debug.WriteLine("  " + commandBarControl2.Caption);
                            }

                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        public void RunAllTests()
        {
            try
            {
                _dteReference.ExecuteCommand("Build.BuildSolution");

                System.Threading.Thread.Sleep(1); // nominate for other threads to recieve cpu power before continuing

                _fieldTestControl.RunAllTests();
            }
            catch { }
        }

        public void RunSelectedTests()
        {
            _fieldTestControl.RunSelectedTests();
        }

        public void NavigateToError(TestDetails testDetails)
        {
            string filename = testDetails.GetFilenameFromMessage();
            if (((EnvDTE80.DTE2)_dteReference).get_IsOpenFile(EnvDTE.Constants.vsViewKindCode, filename))
            {
                var doc = _dteReference.Documents.Item(filename);
                doc.Activate();

                NavigateToLine(doc.Selection as TextSelection, testDetails.GetLineNumberFromMessage());
            }
            else
            {
                var win = _dteReference.OpenFile(EnvDTE.Constants.vsViewKindCode, filename);
                win.Visible = true;
                win.SetFocus();

                NavigateToLine(win.Selection as TextSelection, testDetails.GetLineNumberFromMessage());
            }
        }

        private void NavigateToLine(TextSelection textSelector, int lineNumber)
        {
            if (textSelector != null && lineNumber > 0)
            {
                textSelector.GotoLine(lineNumber);
            }
        }
    }
}