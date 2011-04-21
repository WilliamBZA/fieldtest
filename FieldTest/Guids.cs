// Guids.cs
// MUST match guids.h
using System;

namespace FieldTest
{
    static class GuidList
    {
        public const string guidFieldTestPkgString = "ec328499-1bbe-46b9-9569-9808db4ddbb0";
        public const string guidFieldTestCmdSetString = "5680f43f-677b-447e-a80f-148404af810b";
        public const string guidToolWindowPersistanceString = "b71e09b7-1304-434a-9a0b-1f2b236d7264";
        public const string guidFieldTestCommandSetString = "5680f43f-677b-447e-a80f-148404af810c";

        public static readonly Guid guidFieldTestCmdSet = new Guid(guidFieldTestCmdSetString);
        public static readonly Guid guidFieldTestCommandSet = new Guid(guidFieldTestCommandSetString);
    };
}