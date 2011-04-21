using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FieldTest.Core
{
    public interface IConfigurationManager
    {
        void CopyApplicationConfiguration(string oldConfigFilePath, string newConfigFilePath);
    }
}
