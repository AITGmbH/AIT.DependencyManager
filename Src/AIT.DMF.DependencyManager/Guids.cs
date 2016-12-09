// Guids.cs
// MUST match guids.h
using System;

namespace AIT.AIT_DMF_DependencyManager
{
    static class GuidList
    {
        public const string guidAIT_DMF_DependencyManagerPkgString = "9aeb6ec4-ebde-4d28-9da1-11631f367199";
        public const string guidAIT_DMF_DependencyManagerCmdSetString = "5bfd441a-b291-4961-84e0-ef3365f59f9e";
        public const string guidAIT_DMF_DependencyEditorFactoryString = "93fa4dc3-61ec-47af-b0ba-50cad3caf049";

        public static readonly Guid guidAIT_DMF_DependencyManagerCmdSet = new Guid(guidAIT_DMF_DependencyManagerCmdSetString);
        public static readonly Guid guidAIT_DMF_DependencyEditorFactory = new Guid(guidAIT_DMF_DependencyEditorFactoryString);
    };
}