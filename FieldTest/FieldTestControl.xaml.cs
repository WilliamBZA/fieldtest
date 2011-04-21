using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using FieldTest.Core;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using EnvDTE;
using Microsoft.VisualStudio.Shell.Interop;
using System.Collections.ObjectModel;
using FieldTest.Core.Model;
using System.Threading.Tasks;
using System.ComponentModel;

namespace FieldTest
{
    /// <summary>
    /// Interaction logic for MyControl.xaml
    /// </summary>
    public partial class FieldTestControl : UserControl
    {
        private FieldTestViewModel _viewModel;
        private System.Threading.Tasks.Task _testRunnerTask;
        private IEditorNavigator _navigator;
        private IOutputWriter _outputWriter;
        private IDebugAttacher _debugAttacher;

        public FieldTestControl(IEditorNavigator navigator, IOutputWriter outputWriter, IDebugAttacher debugAttacher)
        {
            _navigator = navigator;
            _outputWriter = outputWriter;
            _debugAttacher = debugAttacher;

            InitializeComponent();

            _viewModel = new FieldTestViewModel();
            _viewModel.CanRunTests = false;

            DataContext = _viewModel;
        }

        public void LoadTestsInSolution(IVsSolution solution)
        {
            _viewModel.LoadTests(solution);
        }

        private void RunAllTests_Click(object sender, RoutedEventArgs e)
        {
            RunAllTests();
        }

        public void RunAllTests()
        {
            if (_viewModel.TestAssemblies != null)
            {
                var allTests = _viewModel.TestAssemblies.SelectMany(t => t.TestClasses).SelectMany(t => t.Tests);

                // run all tests
                RunTests(allTests);
            }
        }

        private void RunSelectedTests_Click(object sender, RoutedEventArgs e)
        {
            RunSelectedTests();
        }

        public void RunSelectedTests()
        {
            if (_viewModel.TestAssemblies != null)
            {
                var selectedTests = _viewModel.TestAssemblies.SelectMany(t => t.TestClasses).SelectMany(t => t.Tests).Where(t => t.IsSelected);

                // run all tests
                RunTests(selectedTests);
            }
        }

        private void DebugSelectedTests_Click(object sender, RoutedEventArgs e)
        {
            // Debug selected tests
            var selectedTests = _viewModel.TestAssemblies.SelectMany(t => t.TestClasses).SelectMany(t => t.Tests).Where(t => t.IsSelected);

            // run all tests
            DebugTests(selectedTests);
        }

        private void DebugAllTests_Click(object sender, RoutedEventArgs e)
        {
            // Debug all tests
            var allTests = _viewModel.TestAssemblies.SelectMany(t => t.TestClasses).SelectMany(t => t.Tests);

            // run all tests
            DebugTests(allTests);
        }

        private void DebugTests(IEnumerable<TestDetails> selectedTests)
        {
            if (_testRunnerTask == null)
            {
                _viewModel.CanRunTests = false;
                _viewModel.TestsAreRunning = true;

                _testRunnerTask = new TaskFactory().StartNew(() =>
                {
                    RemoteTestRunner runner = new RemoteTestRunner(_outputWriter);
                    runner.DebugTests(selectedTests, _debugAttacher);

                    _testRunnerTask = null;

                    _viewModel.CanRunTests = true;
                    _viewModel.TestsAreRunning = false;
                });
            }
        }

        private void RunTests(IEnumerable<TestDetails> selectedTests)
        {
            if (_testRunnerTask == null)
            {
                _viewModel.CanRunTests = false;
                _viewModel.TestsAreRunning = true;

                SetStatus("Running...");

                var testClosure = selectedTests;
                NotifyUnrunTests(testClosure);

                _testRunnerTask = new TaskFactory().StartNew(() =>
                {
                    RemoteTestRunner runner = new RemoteTestRunner(_outputWriter);
                    runner.RunTests(testClosure);

                    _testRunnerTask = null;

                    SetStatus("Done");

                    _viewModel.CanRunTests = true;
                    _viewModel.TestsAreRunning = false;
                });
            }
        }

        private void SetStatus(string statusMessage)
        {
            Dispatcher.Invoke((Action)(() =>
                {
                    Progress.Content = statusMessage;
                }));
        }

        private void TestsInSolution_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (!IsCtrlPressed)
            {
                foreach (var test in _viewModel.TestAssemblies)
                {
                    test.IsSelected = false;
                }
            }

            if ((e.NewValue as BaseTestNavigator) != null)
            {
                (e.NewValue as BaseTestNavigator).IsSelected = !(e.NewValue as BaseTestNavigator).IsSelected;
            }
        }

        private bool IsCtrlPressed
        {
            get
            {
                return Keyboard.IsKeyDown(Key.LeftCtrl)
                    || Keyboard.IsKeyDown(Key.RightCtrl);
            }
        }

        private void NotifyUnrunTests(IEnumerable<TestDetails> selectedTests)
        {
            var allTests = _viewModel.TestAssemblies.SelectMany(t => t.TestClasses).SelectMany(t => t.Tests);

            var testsThatWerentRun = from test
                                     in allTests
                                     where !selectedTests.Contains(test)
                                     select test;

            foreach (var test in testsThatWerentRun)
            {
                test.Status = FieldTestViewModel.SetStatusToPreviouslyRun(test.Status);
            }

            foreach (var test in selectedTests)
            {
                test.Status = TestStatus.InProgress;
            }
        }

        private void FieldTestWindow_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var source = e.OriginalSource as FrameworkElement;
            if (source == null || source.Parent == null)
            {
                return;
            }

            var panel = source.Parent as StackPanel;

            if (panel == null)
            {
                return;
            }

            var testDetails = panel.Tag as TestDetails;
            if (testDetails == null)
            {
                return;
            }

            if (testDetails.Status == TestStatus.Failed || testDetails.Status == TestStatus.FailedPrevious || testDetails.Status == TestStatus.Inconclusive || testDetails.Status == TestStatus.InconclusivePrevious)
            {
                // navigate to error
                _navigator.NavigateToError(testDetails);

                // where is the error?
            }
        }
    }
}