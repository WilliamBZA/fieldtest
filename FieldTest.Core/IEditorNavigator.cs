﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FieldTest.Core.Model;

namespace FieldTest.Core
{
    public interface IEditorNavigator
    {
        void NavigateToError(TestDetails testDetails);
    }
}