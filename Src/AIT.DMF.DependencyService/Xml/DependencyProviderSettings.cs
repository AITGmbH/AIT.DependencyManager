using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using System.Linq;
using AIT.DMF.Contracts.Parser;
using AIT.DMF.Contracts.Exceptions;
using AIT.DMF.Common;
using System.Diagnostics;

namespace AIT.DMF.DependencyService
{
    [Serializable]
    public class DependencyProviderSettings : IDependencyProviderSettings
    {
        [XmlAttribute]
        public DependencyProviderSettingsType Type { get; set; }
        [XmlElement("Setting")]
        //[XmlArrayItem(typeof(DependencyProviderSetting))]
        public List<DependencyProviderSetting> _SettingsList { get; set; }
        [XmlIgnore]
        public List<IDependencyProviderSetting> SettingsList
        {
            get { return _SettingsList.Cast<IDependencyProviderSetting>().ToList(); }
            set { _SettingsList = value.Cast<DependencyProviderSetting>().ToList(); }
        }

        /// <summary>
        /// Gets the setting value based on the name or null if it does not exist.
        /// </summary>
        /// <param name="name">Valid dependency provider settings name</param>
        /// <returns>Value string or null</returns>
        public string GetSettingValue(DependencyProviderValidSettingName name)
        {
            var setting = _SettingsList.Where(x => x.Name.Equals(name)).FirstOrDefault();
            return (null != setting) ? setting.Value : null;
        }

        /// <summary>
        /// Set a new setting value.
        /// </summary>
        /// <param name="name">Valid dependency provider settings name</param>
        public void SetSettingValue(DependencyProviderValidSettingName name, String value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                _SettingsList.RemoveAll(x => x.Name == name);
                return;
            }

            var setting = _SettingsList.Where(x => x.Name.Equals(name)).FirstOrDefault();

            if (setting == null)
            {
                _SettingsList.Add(new DependencyProviderSetting { Name = name, Value = value });
            }
            else
            {
                setting.Value = value;
            }
        }

        /// <summary>
        /// Overloaded methods must implement a provider specific component name string (Abstract method)
        /// </summary>
        /// <returns>Component name string</returns>
        public string GetComponentName()
        {
            // Todo: HACK! Refactor me!!
            switch (Type)
            {
                case DependencyProviderSettingsType.FileShareSettings:
                case DependencyProviderSettingsType.BinaryRepositorySettings:
                    return GetSettingValue(DependencyProviderValidSettingName.ComponentName);
                case DependencyProviderSettingsType.SourceControlSettings:
                case DependencyProviderSettingsType.SourceControlCopySettings:
                    return GetSettingValue(DependencyProviderValidSettingName.ServerRootPath);
                case DependencyProviderSettingsType.VNextBuildResultSettings:
                case DependencyProviderSettingsType.BuildResultSettings:
                    {
                        // Empty IXmlDependency dont have any settings yet to derive component name from
                        if (string.IsNullOrEmpty(GetSettingValue(DependencyProviderValidSettingName.TeamProjectName)) &&
                           string.IsNullOrEmpty(GetSettingValue(DependencyProviderValidSettingName.BuildDefinition)))
                            return "";

                        // Todo: HACK! Should be exception but for the ease of use it is "InvalidSetting"
                        if (string.IsNullOrEmpty(GetSettingValue(DependencyProviderValidSettingName.TeamProjectName)) ||
                           string.IsNullOrEmpty(GetSettingValue(DependencyProviderValidSettingName.BuildDefinition)))
                            return "InvalidSetting";

                        return GetSettingValue(DependencyProviderValidSettingName.TeamProjectName) + "_" + GetSettingValue(DependencyProviderValidSettingName.BuildDefinition);
                    }
                case DependencyProviderSettingsType.SubversionSettings:
                    return GetSettingValue(DependencyProviderValidSettingName.SubversionRootPath);
                default:
                    //Throw an exception if type is unknown to give the developer a hint
                    Logger.Instance().Log(TraceLevel.Error, "Unsupported DependencyProviderSettingsType {0} found!", Type.ToString());
                    throw new DependencyServiceException(string.Format("Unsupported DependencyProviderSettingsType \"{0}\" found!", Type.ToString()));
            }
        }

        /// <summary>
        /// Overloaded methods must implement a provider specific component version string (Abstract method)
        /// </summary>
        /// <returns>Component version string</returns>
        public string GetComponentVersion()
        {
            switch (Type)
            {
                case DependencyProviderSettingsType.FileShareSettings:
                case DependencyProviderSettingsType.BinaryRepositorySettings:
                    return GetSettingValue(DependencyProviderValidSettingName.VersionNumber);
                case DependencyProviderSettingsType.SourceControlSettings:
                case DependencyProviderSettingsType.SourceControlCopySettings:
                case DependencyProviderSettingsType.SubversionSettings:
                    return GetSettingValue(DependencyProviderValidSettingName.VersionSpec);
                case DependencyProviderSettingsType.VNextBuildResultSettings:
                    var buildNumberExist = !string.IsNullOrEmpty(GetSettingValue(DependencyProviderValidSettingName.BuildNumber));
                    var buildStatusExist = !string.IsNullOrEmpty(GetSettingValue(DependencyProviderValidSettingName.BuildStatus));
                    var buildTagsExist = !string.IsNullOrEmpty(GetSettingValue(DependencyProviderValidSettingName.BuildTags));
                    var versionStrings = new List<string>();
                    if (!buildNumberExist && !buildStatusExist && !buildTagsExist)
                    {
                        return string.Empty;
                    }
                    else if (buildNumberExist)
                    {
                        return GetSettingValue(DependencyProviderValidSettingName.BuildNumber);
                    }
                    else if (!buildStatusExist && !buildTagsExist)
                    {
                        return "InvalidSetting";
                    }
                    else if (buildStatusExist)
                    {
                        versionStrings.Add("Status:" + GetSettingValue(DependencyProviderValidSettingName.BuildStatus));
                    }
                    else if (buildTagsExist)
                    {
                        versionStrings.Add("Tags:" + GetSettingValue(DependencyProviderValidSettingName.BuildTags));
                    }
                    return string.Join(";", versionStrings);


                case DependencyProviderSettingsType.BuildResultSettings:
                    {
                        // Empty IXmlDependency dont have any settings yet to derive version string from
                        if (string.IsNullOrEmpty(GetSettingValue(DependencyProviderValidSettingName.BuildNumber)) &&
                            string.IsNullOrEmpty(GetSettingValue(DependencyProviderValidSettingName.BuildQuality)) &&
                            string.IsNullOrEmpty(GetSettingValue(DependencyProviderValidSettingName.BuildStatus)))
                        {
                            return "";
                        }

                        if (!string.IsNullOrEmpty(GetSettingValue(DependencyProviderValidSettingName.BuildNumber)))
                        {
                            return GetSettingValue(DependencyProviderValidSettingName.BuildNumber);
                        }

                        if (string.IsNullOrEmpty(GetSettingValue(DependencyProviderValidSettingName.BuildQuality)) &&
                           string.IsNullOrEmpty(GetSettingValue(DependencyProviderValidSettingName.BuildStatus)))
                        {
                            return "InvalidSetting";
                        }

                        var versionStringArray = new List<string>();

                        if (!string.IsNullOrEmpty(GetSettingValue(DependencyProviderValidSettingName.BuildQuality)))
                        {
                            versionStringArray.Add("Quality:" + GetSettingValue(DependencyProviderValidSettingName.BuildQuality));
                        }
                        if (!string.IsNullOrEmpty(GetSettingValue(DependencyProviderValidSettingName.BuildStatus)))
                        {
                            versionStringArray.Add("Status:" + GetSettingValue(DependencyProviderValidSettingName.BuildStatus));
                        }

                        return string.Join(";", versionStringArray);
                    }
                default:
                    //Throw an exception if type is unknown to give the developer a hint
                    Logger.Instance().Log(TraceLevel.Error, "Unsupported DependencyProviderSettingsType {0} found!", Type.ToString());
                    throw new DependencyServiceException(string.Format("Unsupported DependencyProviderSettingsType \"{0}\" found!", Type.ToString()));
            }
        }
    }
}
