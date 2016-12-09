namespace AIT.DMF.Contracts.Common
{
    public interface IComponentName
    {
        /// <summary>
        /// Represents the Name of the component in the file share or the full path (Path + Name) to the component in source control.
        /// </summary>
        string Path { get; }

        /// <summary>
        /// Represents the teamProject (which is together with the build definition) used to identify a build result component.
        /// </summary>
        string TeamProject { get; }

        /// <summary>
        /// Represents the build definition name.
        /// </summary>
        string BuildDefinition { get; }

        /// <summary>
        /// Gets the name represented by path or team project and build definition
        /// </summary>
        /// <returns>Component name</returns>
        string GetName();
    }
}
