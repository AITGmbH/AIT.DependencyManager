using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using System.Xml.Serialization;
using AIT.DMF.Common;
using AIT.DMF.Contracts.Enums;
using AIT.DMF.Contracts.Exceptions;
using AIT.DMF.Contracts.Parser;

namespace AIT.DMF.DependencyService
{
    internal class ParserXml
    {
        #region Private members

        private string _validationError;

        #endregion

        /// <summary>
        /// Creates the dependency XML serializer.
        /// </summary>
        /// <returns>Initialized XML serializer</returns>
        private static XmlSerializer CreateDependencyXmlSerializer()
        {
            // Handle enums
            var xAttrOver = new XmlAttributeOverrides();
            var xAttrs = new XmlAttributes();
            var xDependencyTypeEnum = new XmlEnumAttribute {Name = "Source"};
            xAttrs.XmlEnum = xDependencyTypeEnum;
            xAttrOver.Add(typeof(DependencyType), "Source", xAttrs);
            xAttrs = new XmlAttributes();
            xDependencyTypeEnum = new XmlEnumAttribute {Name = "Binary"};
            xAttrs.XmlEnum = xDependencyTypeEnum;
            xAttrOver.Add(typeof(DependencyType), "Binary", xAttrs);
            xAttrs = new XmlAttributes();
            var xDependencyProviderSettingsTypeEnum = new XmlEnumAttribute {Name = "SourceControlSettings"};
            xAttrs.XmlEnum = xDependencyProviderSettingsTypeEnum;
            xAttrOver.Add(typeof(DependencyProviderSettingsType), "SourceControlSettings", xAttrs);
            xAttrs = new XmlAttributes();
            xDependencyProviderSettingsTypeEnum = new XmlEnumAttribute { Name = "SourceControlCopySettings" };
            xAttrs.XmlEnum = xDependencyProviderSettingsTypeEnum;
            xAttrOver.Add(typeof(DependencyProviderSettingsType), "SourceControlCopySettings", xAttrs);
            xAttrs = new XmlAttributes();
            xDependencyProviderSettingsTypeEnum = new XmlEnumAttribute {Name = "BuildResultSettings"};
            xAttrs.XmlEnum = xDependencyProviderSettingsTypeEnum;
            xAttrOver.Add(typeof(DependencyProviderSettingsType), "BuildResultSettings", xAttrs);
            xAttrs = new XmlAttributes();
            xDependencyProviderSettingsTypeEnum = new XmlEnumAttribute {Name = "FileShareSettings"};
            xAttrs.XmlEnum = xDependencyProviderSettingsTypeEnum;
            xAttrOver.Add(typeof(DependencyProviderSettingsType), "FileShareSettings", xAttrs);
            xAttrs = new XmlAttributes();
            xDependencyProviderSettingsTypeEnum = new XmlEnumAttribute {Name = "BinaryRepositorySettings"};
            xAttrs.XmlEnum = xDependencyProviderSettingsTypeEnum;
            xAttrOver.Add(typeof(DependencyProviderSettingsType), "BinaryRepositorySettings", xAttrs);
            xAttrs = new XmlAttributes();
            var xDependencyProviderValidSettingNameEnum = new XmlEnumAttribute {Name = "ServerRootPath"};
            xAttrs.XmlEnum = xDependencyProviderValidSettingNameEnum;
            xAttrOver.Add(typeof(DependencyProviderSettingsType), "ServerRootPath", xAttrs);
            xAttrs = new XmlAttributes();
            xDependencyProviderValidSettingNameEnum = new XmlEnumAttribute {Name = "IncludeFilter"};
            xAttrs.XmlEnum = xDependencyProviderValidSettingNameEnum;
            xAttrOver.Add(typeof(DependencyProviderSettingsType), "IncludeFilter", xAttrs);
            xAttrs = new XmlAttributes();
            xDependencyProviderValidSettingNameEnum = new XmlEnumAttribute { Name = "ExcludedFiles" };
            xAttrs.XmlEnum = xDependencyProviderValidSettingNameEnum;
            xAttrOver.Add(typeof(DependencyProviderSettingsType), "ExcludedFiles", xAttrs);
            xAttrs = new XmlAttributes();
            xDependencyProviderValidSettingNameEnum = new XmlEnumAttribute { Name = "IncludeFoldersFilter" };
            xAttrs.XmlEnum = xDependencyProviderValidSettingNameEnum;
            xAttrOver.Add(typeof(DependencyProviderSettingsType), "IncludeFoldersFilter", xAttrs);
            xAttrs = new XmlAttributes();
            xDependencyProviderValidSettingNameEnum = new XmlEnumAttribute { Name = "ExcludeFoldersFilter" };
            xAttrs.XmlEnum = xDependencyProviderValidSettingNameEnum;
            xAttrOver.Add(typeof(DependencyProviderSettingsType), "ExcludeFoldersFilter", xAttrs);
            xAttrs = new XmlAttributes();
            xDependencyProviderValidSettingNameEnum = new XmlEnumAttribute {Name = "FileShareRootPath"};
            xAttrs.XmlEnum = xDependencyProviderValidSettingNameEnum;
            xAttrOver.Add(typeof(DependencyProviderSettingsType), "FileShareRootPath", xAttrs);
            xAttrs = new XmlAttributes();
            xDependencyProviderValidSettingNameEnum = new XmlEnumAttribute {Name = "ComponentName"};
            xAttrs.XmlEnum = xDependencyProviderValidSettingNameEnum;
            xAttrOver.Add(typeof(DependencyProviderSettingsType), "ComponentName", xAttrs);
            xAttrs = new XmlAttributes();
            xDependencyProviderValidSettingNameEnum = new XmlEnumAttribute {Name = "VersionNumber"};
            xAttrs.XmlEnum = xDependencyProviderValidSettingNameEnum;
            xAttrOver.Add(typeof(DependencyProviderSettingsType), "VersionNumber", xAttrs);
            xAttrs = new XmlAttributes();
            xDependencyProviderValidSettingNameEnum = new XmlEnumAttribute {Name = "VersionSpec"};
            xAttrs.XmlEnum = xDependencyProviderValidSettingNameEnum;
            xAttrOver.Add(typeof(DependencyProviderSettingsType), "VersionSpec", xAttrs);
            xAttrs = new XmlAttributes();
            xDependencyProviderValidSettingNameEnum = new XmlEnumAttribute {Name = "TeamProjectName"};
            xAttrs.XmlEnum = xDependencyProviderValidSettingNameEnum;
            xAttrOver.Add(typeof(DependencyProviderSettingsType), "TeamProjectName", xAttrs);
            xAttrs = new XmlAttributes();
            xDependencyProviderValidSettingNameEnum = new XmlEnumAttribute {Name = "BuildDefinition"};
            xAttrs.XmlEnum = xDependencyProviderValidSettingNameEnum;
            xAttrOver.Add(typeof(DependencyProviderSettingsType), "BuildDefinition", xAttrs);
            xAttrs = new XmlAttributes();
            xDependencyProviderValidSettingNameEnum = new XmlEnumAttribute {Name = "BuildNumber"};
            xAttrs.XmlEnum = xDependencyProviderValidSettingNameEnum;
            xAttrOver.Add(typeof(DependencyProviderSettingsType), "BuildNumber", xAttrs);
            xAttrs = new XmlAttributes();
            xDependencyProviderValidSettingNameEnum = new XmlEnumAttribute {Name = "BuildQuality"};
            xAttrs.XmlEnum = xDependencyProviderValidSettingNameEnum;
            xAttrOver.Add(typeof(DependencyProviderSettingsType), "BuildQuality", xAttrs);
            xAttrs = new XmlAttributes();
            xDependencyProviderValidSettingNameEnum = new XmlEnumAttribute {Name = "BuildStatus"};
            xAttrs.XmlEnum = xDependencyProviderValidSettingNameEnum;
            xAttrOver.Add(typeof(DependencyProviderSettingsType), "BuildStatus", xAttrs);
            xAttrs = new XmlAttributes();
            xDependencyProviderValidSettingNameEnum = new XmlEnumAttribute {Name = "BinaryTeamProjectCollectionUrl"};
            xAttrs.XmlEnum = xDependencyProviderValidSettingNameEnum;
            xAttrOver.Add(typeof(DependencyProviderSettingsType), "BinaryTeamProjectCollectionUrl", xAttrs);
            xAttrs = new XmlAttributes();
            xDependencyProviderValidSettingNameEnum = new XmlEnumAttribute {Name = "BinaryRepositoryTeamProject"};
            xAttrs.XmlEnum = xDependencyProviderValidSettingNameEnum;
            xAttrOver.Add(typeof(DependencyProviderSettingsType), "BinaryRepositoryTeamProject", xAttrs);
            xAttrs = new XmlAttributes();
            xDependencyProviderValidSettingNameEnum = new XmlEnumAttribute {Name = "RelativeOutputPath"};
            xAttrs.XmlEnum = xDependencyProviderValidSettingNameEnum;
            xAttrOver.Add(typeof(DependencyProviderSettingsType), "RelativeOutputPath", xAttrs);
            xAttrs = new XmlAttributes();
            xDependencyProviderValidSettingNameEnum = new XmlEnumAttribute {Name = "FolderMappings"};
            xAttrs.XmlEnum = xDependencyProviderValidSettingNameEnum;
            xAttrOver.Add(typeof(DependencyProviderSettingsType), "FolderMappings", xAttrs);
            xAttrs = new XmlAttributes();
            xDependencyProviderValidSettingNameEnum = new XmlEnumAttribute { Name = "CompressedDependency" };
            xAttrs.XmlEnum = xDependencyProviderValidSettingNameEnum;
            xAttrOver.Add(typeof(DependencyProviderSettingsType), "CompressedDependency", xAttrs);
            xAttrs = new XmlAttributes();
            xDependencyProviderValidSettingNameEnum = new XmlEnumAttribute { Name = "IgnoreInSideBySideAnomalyChecks" };
            xAttrs.XmlEnum = xDependencyProviderValidSettingNameEnum;
            xAttrOver.Add(typeof(DependencyProviderSettingsType), "IgnoreInSideBySideAnomalyChecks", xAttrs);

            var serializer = new XmlSerializer(typeof(XmlComponent), xAttrOver);
            serializer.UnknownNode += SerializerUnknownNode;
            serializer.UnknownAttribute += SerializerUnknownAttribute;

            return serializer;
        }

        /// <summary>
        /// Reads the dependency definition XML Document.
        /// </summary>
        /// <param name="dependencyDefinitionXmlDocument">The dependency definition xml document.</param>
        /// <returns>IXmlComponent read from the dependency definition file</returns>
        public IXmlComponent ReadDependencyFile(XDocument dependencyDefinitionXmlDocument)
        {
            if (dependencyDefinitionXmlDocument == null)
                throw new ArgumentNullException("dependencyDefinitionXmlDocument");

            var serializer = CreateDependencyXmlSerializer();
            var assembly = GetType().Assembly;

            // using the reflected assembly name does not work with ILMerge for the WorkFlow-Activity
            var schemaName = "AIT.DMF.DependencyService.Xsd.AITDependency.xsd";
            try
            {
                // Load schema from assembly
                var xsdStreamReader = new StreamReader(assembly.GetManifestResourceStream(schemaName));

                // Enable schema validation.
                var schemaSet = new XmlSchemaSet();
                schemaSet.Add("http://schemas.aitgmbh.de/DependencyManager/2011/11", XmlReader.Create(xsdStreamReader));
                var settings = new XmlReaderSettings {ValidationType = ValidationType.Schema, Schemas = schemaSet};
                settings.ValidationEventHandler += new ValidationEventHandler(settings_ValidationEventHandler);
                Logger.Instance().Log(TraceLevel.Verbose, "Loading schema validation file from assembly finished successfully");

                _validationError = null;
                var reader = XmlReader.Create(dependencyDefinitionXmlDocument.Root.CreateReader(), settings);

                var comp = (IXmlComponent)serializer.Deserialize(reader);
                Logger.Instance().Log(TraceLevel.Info, "Parsing dependency definition xml finished successfully");

                return comp;
            }
            catch (ArgumentNullException)
            {
                Logger.Instance().Log(TraceLevel.Error, string.Format(CultureInfo.InvariantCulture, "Could not load schema validation file from assembly: {0}", schemaName));
                throw new DependencyServiceException(string.Format(CultureInfo.InvariantCulture, "Could not load schema validation file from assembly: {0}", schemaName));
            }
            catch (InvalidOperationException ioe)
            {
                if (_validationError != null)
                {
                    Logger.Instance().Log(TraceLevel.Error, string.Format("Invalid XML found while parsing dependency definition xml (Validation error: {0})",_validationError));
                    throw new DependencyServiceException(string.Format("Invalid XML found while parsing dependency definition xml ({0})", _validationError));
                }

                Logger.Instance().Log(TraceLevel.Error, string.Format("Unknown error while parse dependency definition xml (Invalid operation error: {0})", ioe.Message));
                throw new DependencyServiceException(string.Format("Unknown error while parse dependency definition xml ({0}", ioe.Message));
            }
        }

        /// <summary>
        /// Fetches the XDocument and returns the IXMLComponent object read from the XDocument.
        /// </summary>
        /// <param name="path">The path to the dependency definition file.</param>
        /// <returns>IXmlComponent read from the dependency definition file</returns>
        public IXmlComponent ReadDependencyFile(string path)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentNullException("path");

            Logger.Instance().Log(TraceLevel.Info, string.Format("Parsing dependency definition file {0} ...", path));
            var xdoc = XDocument.Load(path);
            return ReadDependencyFile(xdoc);
        }

        /// <summary>
        /// Serializes the IXmlComponent object to the dependency dependency file.
        /// </summary>
        /// <param name="component">The component.</param>
        /// <param name="path">The path.</param>
        internal void StoreDependencyFile(IXmlComponent component, string path)
        {
            if (null == component)
                throw new ArgumentNullException("component");

            if (string.IsNullOrEmpty(path))
                throw new ArgumentNullException("path");

            Logger.Instance().Log(TraceLevel.Info, string.Format("Writing dependency definition file {0} ...", path));

            var serializer = CreateDependencyXmlSerializer();

            if (File.Exists(path))
            {
                // make sure we write a fresh file to prevent partial writes/remaining fragments from previous versions
                File.Delete(path);
            }

            using (var sw = File.OpenWrite(path))
            {
                serializer.Serialize(sw, component);
            }
            Logger.Instance().Log(TraceLevel.Info, string.Format("Writing information to dependency definition file finished successfully"));
        }

        /// <summary>
        /// Creates an empty IXmlDependency object.
        /// </summary>
        /// <param name="dependencyType">The type of the dependency.</param>
        /// <returns>Empty IXmlDependency object</returns>
        public IXmlDependency CreateEmptyIXmlDependency(string dependencyType)
        {
            if (string.IsNullOrEmpty(dependencyType))
                throw new ArgumentNullException("dependencyType");

            // Todo: HACK This should be constructed without specific knowledge about the type
            var xmlDep = new XmlDependency
                             {
                                 Type =
                                     dependencyType.Equals("SourceControl")
                                         ? DependencyType.SourceDependency
                                         : DependencyType.BinaryDependency,
                                 ProviderConfiguration = new DependencyProviderConfig
                                                             {
                                                                 Type
                                                                     =
                                                                     dependencyType,
                                                                 Settings
                                                                     =
                                                                     new DependencyProviderSettings
                                                                         {
                                                                             SettingsList
                                                                                 =
                                                                                 new List
                                                                                 <
                                                                                 IDependencyProviderSetting
                                                                                 >
                                                                                 ()
                                                                         }
                                                             }
                             };
            if (dependencyType.Equals("SourceControl"))
                xmlDep.ProviderConfiguration.Settings.Type = DependencyProviderSettingsType.SourceControlSettings;
            else if (dependencyType.Equals("SourceControlDownload"))
                xmlDep.ProviderConfiguration.Settings.Type = DependencyProviderSettingsType.SourceControlCopySettings;

            if (dependencyType.Equals("BinaryRepository"))
                xmlDep.ProviderConfiguration.Settings.Type = DependencyProviderSettingsType.BinaryRepositorySettings;
            else if (dependencyType.Equals("BuildResultJSON"))
                xmlDep.ProviderConfiguration.Settings.Type = DependencyProviderSettingsType.VNextBuildResultSettings;
            else if (dependencyType.Equals("BuildResult"))
                xmlDep.ProviderConfiguration.Settings.Type = DependencyProviderSettingsType.BuildResultSettings;
            else if (dependencyType.Equals("FileShare"))
                xmlDep.ProviderConfiguration.Settings.Type = DependencyProviderSettingsType.FileShareSettings;
            else if (dependencyType.Equals("Subversion"))
                xmlDep.ProviderConfiguration.Settings.Type = DependencyProviderSettingsType.SubversionSettings;

            return xmlDep;
        }

        #region Helpers

        /// <summary>
        /// Handles the ValidationEventHandler event of the settings control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Xml.Schema.ValidationEventArgs"/> instance containing the event data.</param>
        void settings_ValidationEventHandler(object sender, ValidationEventArgs e)
        {
            Logger.Instance().Log(TraceLevel.Verbose, string.Format("XML Validation error: {0}", e.Message));
            try
            {
                _validationError = e.Message.Substring(0, e.Message.LastIndexOf('-'));
            }
            catch (ArgumentOutOfRangeException)
            {
                _validationError = e.Message;
            }
        }

        /// <summary>
        /// Handles the UnknownAttribute event of the serializer control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Xml.Serialization.XmlAttributeEventArgs"/> instance containing the event data.</param>
        private static void SerializerUnknownAttribute(object sender, XmlAttributeEventArgs e)
        {
            Logger.Instance().Log(TraceLevel.Verbose, string.Format("Unknown attribute {0} found at {1}:{2}.", e.Attr, e.LineNumber, e.LinePosition));
        }

        /// <summary>
        /// Handles the UnknownNode event of the serializer control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Xml.Serialization.XmlNodeEventArgs"/> instance containing the event data.</param>
        private static void SerializerUnknownNode(object sender, XmlNodeEventArgs e)
        {
            Logger.Instance().Log(TraceLevel.Verbose, string.Format("Ignoring node {0} {1}", e.Name, e.Text));
        }

        #endregion
    }
}
