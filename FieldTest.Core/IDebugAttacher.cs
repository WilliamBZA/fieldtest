using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FieldTest.Core
{
    public interface IDebugAttacher
    {
        bool AttachToProcess(int processId);
    }
}
