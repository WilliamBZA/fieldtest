// PkgCmdID.cs
// MUST match PkgCmdID.h
using System;
using System.Data.SqlClient;

namespace FieldTest
{
    static class PkgCmdIDList
    {
        public const uint cmdIdFieldTest      = 0x0101;
        public const uint cmdRunAllTests      = 0x0300;
        public const uint cmdRunSelectedTests = 0x0301;
    };
}