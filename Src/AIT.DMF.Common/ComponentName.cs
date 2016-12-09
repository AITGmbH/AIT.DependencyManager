using System;
using AIT.DMF.Contracts.Common;
using AIT.DMF.Contracts.Exceptions;

namespace AIT.DMF.Common
{
    public class ComponentName : IComponentName
    {
        #region Constructors
        /// <summary>
        /// Initializes the component name with a name or path (for file share or source control).
        /// </summary>
        /// <param name="pathOrName">Path and/or name of component</param>
        public ComponentName(string pathOrName)
        {
            if(string.IsNullOrEmpty(pathOrName))
            {
                throw new InvalidComponentException("Component name was initialized with an invalid name (Path was null)");
            }
            
            Path = pathOrName;
            TeamProject = null;
            BuildDefinition = null;
        }

        /// <summary>
        /// Initializes the component name with a team project name and a build definition name (for build drop location).
        /// </summary>
        /// <param name="teamProjectName"></param>
        /// <param name="buildDefinitionName"></param>
        public ComponentName(string teamProjectName, string buildDefinitionName)
        {
            if (string.IsNullOrEmpty(teamProjectName) || string.IsNullOrEmpty(buildDefinitionName))
            {
                throw new InvalidComponentException("Component name was initialized with an invalid name (Team project or build definition was null)");
            }

            TeamProject = teamProjectName;
            BuildDefinition = buildDefinitionName;
            Path = null;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Returns the path/name for the component (in source control/file share).
        /// </summary>
        public string Path { get; private set; }

        /// <summary>
        /// Returns the team project for the component (in build server).
        /// </summary>
        public string TeamProject { get; private set; }

        /// <summary>
        /// Returns the build definition for the component (in build server).
        /// </summary>
        public string BuildDefinition { get; private set; }

        #endregion

        #region Public methods

        /// <summary>
        /// Returns the name of this component.
        /// </summary>
        /// <returns>Component name</returns>
        public string GetName()
        {
            if (!String.IsNullOrEmpty(Path))
            {
                return Path;
            }

            return String.Format("{0}_{1}", TeamProject, BuildDefinition);
        }

        public override string ToString()
        {
            return GetName();
        }

        #endregion
    }
}
