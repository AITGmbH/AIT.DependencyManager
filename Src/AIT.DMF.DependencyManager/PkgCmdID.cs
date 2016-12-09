// PkgCmdID.cs
// MUST match PkgCmdID.h

namespace AIT.AIT_DMF_DependencyManager
{
    static class PkgCmdIDList
    {
        public const uint cmdidUserHelpToolsMenu = 0x103;
        public const uint cmdidEditGeneralSettingsToolsMenu = 0x104;
        public const uint cmdidEditPersonalSettingsToolsMenu = 0x116;

        public const uint cmdidGetDependenciesRecursiveSolution = 0x105;
        public const uint cmdidCleanDependenciesSolution = 0x106;
        public const uint cmdidForcedGetDependenciesRecursiveSolution = 0x107;
        public const uint cmdidGetDirectDependenciesSolution = 0x108;
        public const uint cmdidForcedGetDirectDependenciesSolution = 0x109;

        public const uint cmdidGetDependenciesRecursiveSourceControl = 0x110;
        public const uint cmdidCleanDependenciesSourceControl = 0x111;
        public const uint cmdidCreateComponentTargetsSourceControl = 0x112;
        public const uint cmdidForcedGetDependenciesRecursiveSourceControl = 0x113;
        public const uint cmdidGetDirectDependenciesSourceControl = 0x114;
        public const uint cmdidForcedGetDirectDependenciesSourceControl = 0x115;
    };
}