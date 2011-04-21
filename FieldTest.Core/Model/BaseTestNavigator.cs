using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace FieldTest.Core.Model
{
    [DataContract]
    public abstract class BaseTestNavigator : INotifyPropertyChanged
    {
        public BaseTestNavigator()
        {
            Status = TestStatus.NotRunPrevious;
        }

        private bool _isSelected;
        public virtual bool IsSelected
        {
            get { return _isSelected; }

            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;

                    if (PropertyChanged != null)
                    {
                        PropertyChanged(this, new PropertyChangedEventArgs("IsSelected"));
                    }
                }
            }
        }

        private TestStatus _testStatus;

        [DataMember]
        public virtual TestStatus Status
        {
            get { return _testStatus; }

            set
            {
                if (_testStatus != value)
                {
                    _testStatus = value;

                    if (PropertyChanged != null)
                    {
                        PropertyChanged(this, new PropertyChangedEventArgs("Status"));
                    }
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}