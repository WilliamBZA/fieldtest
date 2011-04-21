using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;
using FieldTest.Core.Model;

namespace FieldTest
{
    public class TestStatusConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            TestStatus status = (TestStatus)value;

            if (status == TestStatus.InProgress)
            {
                status = TestStatus.NotRun;
            }

            return string.Format("/FieldTest;component/Resources/{0}.png", status);
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}