// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DownloaderFactoryTest.cs" company="AIT GmbH & Co. KG.">
//   All rights reserved by AIT GmbH & Co. KG.
// </copyright>
// <summary>
//   This is a test class for DownloaderFactory and is intended
//   to contain all DownloaderFactory Unit Tests
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System;

namespace AIT.DMF.PluginFactory.Test
{
    using System.Collections.Generic;
    using Contracts.Exceptions;
    using Contracts.Graph;
    using Contracts.Parser;
    using DependencyService;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// This is a test class for DownloaderFactoryTest and is intended
    /// to contain all DownloaderFactoryTest Unit Tests
    /// </summary>
    [TestClass]
    public class DownloaderFactoryTest
    {
        #region Downloader_FileShareCopy

        /// <summary>
        /// Generates a downloader of type Downloader_FileShareCopy based on the downloader name.
        /// </summary>
        [TestMethod]
        public void GetDownloaderFileShareCopyDownloaderNameTest()
        {
            var target = new DownloaderFactory();
            const string downloaderName = "Downloader_FileShareCopy";

            var actual = target.GetDownloader(downloaderName);
            Assert.AreNotEqual(actual, null);
            Assert.AreEqual(actual.DownloadType, downloaderName);
        }

        /// <summary>
        /// Generates a downloader of type Downloader_FileShareCopy based on the downloader name.
        /// </summary>
        [TestMethod]
        public void GetDownloaderFileShareCopyAliasDownloaderNameTest()
        {
            var target = new DownloaderFactory();
            const string downloaderName = "Downloader_FileShare";

            var actual = target.GetDownloader(downloaderName);
            Assert.AreNotEqual(actual, null);
            Assert.AreEqual(actual.DownloadType, "Downloader_FileShareCopy");
        }

        /// <summary>
        /// Generates a downloader of type Downloader_FileShareCopy based on the ComponentType value.
        /// </summary>
        [TestMethod]
        public void GetDownloaderFileShareComponentTypeTest()
        {
            var target = new DownloaderFactory();
            IComponent componentType =
                new Component(
                    new DependencyProviderConfig
                        {
                            Type = ComponentType.FileShare.ToString(),
                            Settings =
                                new DependencyProviderSettings
                                    {
                                        SettingsList =
                                            new List<IDependencyProviderSetting>
                                                {
                                                    new DependencyProviderSetting
                                                        {
                                                            Name = DependencyProviderValidSettingName.ComponentName,
                                                            Value = "Test"
                                                        },
                                                    new DependencyProviderSetting
                                                        {
                                                            Name = DependencyProviderValidSettingName.VersionNumber,
                                                            Value = "123"
                                                        }
                                                }
                                    }
                        });

            var actual = target.GetDownloader(componentType);
            Assert.AreNotEqual(actual, null);
            Assert.AreEqual(actual.DownloadType, "Downloader_FileShareCopy");
        }

        // Downloader_FileShareCopy based on the ComponentType "BuildResult" requires infrastructure.
        #endregion

        #region Downloader_SourceControlCopy

        /// <summary>
        /// Generates a downloader of type Downloader_SourceControlCopy based on the downloader name.
        /// </summary>
        [TestMethod]
        public void GetDownloaderSourceControlCopyDownloaderNameTest()
        {
            var target = new DownloaderFactory();
            const string downloaderName = "Downloader_SourceControlCopy";

            var actual = target.GetDownloader(downloaderName);
            Assert.AreNotEqual(actual, null);
            Assert.AreEqual(actual.DownloadType, downloaderName);
        }

        /// <summary>
        /// Generates a downloader of type Downloader_SourceControlCopy based on the downloader name.
        /// </summary>
        [TestMethod]
        public void GetDownloaderSourceControlCopyAliasDownloaderNameTest()
        {
            var target = new DownloaderFactory();
            const string downloaderName = "Downloader_BinaryRepository";

            var actual = target.GetDownloader(downloaderName);
            Assert.AreNotEqual(actual, null);
            Assert.AreEqual(actual.DownloadType, "Downloader_SourceControlCopy");
        }

        // Downloader_SourceControlCopy based on the ComponentType "SourceControlCopy" requires infrastructure.
        // Downloader_SourceControlCopy based on the ComponentType "BinaryRepository" requires infrastructure.
        #endregion

        #region Downloader_SourceControlMapping

        /// <summary>
        /// Generates a downloader of type Downloader_SourceControlMapping based on the downloader name.
        /// </summary>
        [TestMethod]
        public void GetDownloaderSourceControlMappingDownloaderNameTest()
        {
            var target = new DownloaderFactory();
            const string downloaderName = "Downloader_SourceControlMapping";

            var actual = target.GetDownloader(downloaderName);
            Assert.AreNotEqual(actual, null);
            Assert.AreEqual(actual.DownloadType, downloaderName);
        }

        /// <summary>
        /// Generates a downloader of type Downloader_SourceControlMapping based on the downloader name.
        /// </summary>
        [TestMethod]
        public void GetDownloaderSourceControlMappingAliasDownloaderNameTest()
        {
            var target = new DownloaderFactory();
            const string downloaderName = "Downloader_SourceControl";

            var actual = target.GetDownloader(downloaderName);
            Assert.AreNotEqual(actual, null);
            Assert.AreEqual(actual.DownloadType, "Downloader_SourceControlMapping");
        }

        // Downloader_SourceControlMapping based on the ComponentType "SourceControl" requires infrastructure.
        #endregion

        #region Downloader_ZippedDependency
        
        /// <summary>
        /// Generates a downloader of type Downloader_ZippedDependency based on the downloader name.
        /// </summary>
        [TestMethod]
        public void GetDownloaderZippedDependencyDownloaderNameTest()
        {
            var target = new DownloaderFactory();
            const string downloaderName = "Downloader_ZippedDependency";

            var actual = target.GetDownloader(downloaderName);
            Assert.AreNotEqual(actual, null);
            Assert.AreEqual(actual.DownloadType, downloaderName);
        }

        /// <summary>
        /// Generates a downloader of type Downloader_ZippedDependency based on the ComponentType FileShare.
        /// </summary>
        [TestMethod]
        public void GetDownloaderFileShareZippedDependencyComponentTypeTestNoDeletionSetting_ShouldBeFalse()
        {
            var target = new DownloaderFactory();
            IComponent componentType =
                new Component(
                    new DependencyProviderConfig
                    {
                        Type = ComponentType.FileShare.ToString(),
                        Settings =
                            new DependencyProviderSettings
                            {
                                SettingsList =
                                    new List<IDependencyProviderSetting>
                                                {
                                                    new DependencyProviderSetting
                                                        {
                                                            Name = DependencyProviderValidSettingName.ComponentName,
                                                            Value = "Test"
                                                        },
                                                    new DependencyProviderSetting
                                                        {
                                                            Name = DependencyProviderValidSettingName.VersionNumber,
                                                            Value = "123"
                                                        },
                                                    new DependencyProviderSetting
                                                        {
                                                            Name = DependencyProviderValidSettingName.CompressedDependency,
                                                            Value = "True"
                                                        }
                                                }
                            }
                    });
            object actual = null;
            
            try
            {
                actual = target.GetDownloader(componentType);
              }
            catch (Exception ae)
            {
                Assert.AreEqual("The settings is missing that determines if the archive files should be deleted, please make sure it is set accordingly", ae.Message);
            }
        
   
            Assert.IsNotNull(actual);
            
    
        }

        /// <summary>
        /// Generates a downloader of type Downloader_ZippedDependency based on the ComponentType FileShare.
        /// </summary>
        [TestMethod]
        public void GetDownloaderFileShareZippedDependencyComponentTypeTestDeleteArchives()
        {
            var target = new DownloaderFactory();
            IComponent componentType =
                new Component(
                    new DependencyProviderConfig
                    {
                        Type = ComponentType.FileShare.ToString(),
                        Settings =
                            new DependencyProviderSettings
                            {
                                SettingsList =
                                    new List<IDependencyProviderSetting>
                                                {
                                                    new DependencyProviderSetting
                                                        {
                                                            Name = DependencyProviderValidSettingName.ComponentName,
                                                            Value = "Test"
                                                        },
                                                    new DependencyProviderSetting
                                                        {
                                                            Name = DependencyProviderValidSettingName.VersionNumber,
                                                            Value = "123"
                                                        },
                                                    new DependencyProviderSetting
                                                        {
                                                            Name = DependencyProviderValidSettingName.CompressedDependency,
                                                            Value = "True"
                                                        },
                                                    new DependencyProviderSetting
                                                        {
                                                            Name = DependencyProviderValidSettingName.DeleteArchiveFiles,
                                                            Value = "True"
                                                        }
                                                }
                            }
                    });

            var actual = target.GetDownloader(componentType);
            Assert.AreNotEqual(actual, null);
            Assert.AreEqual(actual.DownloadType, "Downloader_ZippedDependency");
        }

        /// <summary>
        /// Generates a downloader of type Downloader_ZippedDependency based on the ComponentType FileShare.
        /// </summary>
        [TestMethod]
        public void GetDownloaderFileShareZippedDependencyComponentTypeTestDoNotDeleteArchives()
        {
            var target = new DownloaderFactory();
            IComponent componentType =
                new Component(
                    new DependencyProviderConfig
                    {
                        Type = ComponentType.FileShare.ToString(),
                        Settings =
                            new DependencyProviderSettings
                            {
                                SettingsList =
                                    new List<IDependencyProviderSetting>
                                                {
                                                    new DependencyProviderSetting
                                                        {
                                                            Name = DependencyProviderValidSettingName.ComponentName,
                                                            Value = "Test"
                                                        },
                                                    new DependencyProviderSetting
                                                        {
                                                            Name = DependencyProviderValidSettingName.VersionNumber,
                                                            Value = "123"
                                                        },
                                                    new DependencyProviderSetting
                                                        {
                                                            Name = DependencyProviderValidSettingName.CompressedDependency,
                                                            Value = "True"
                                                        },
                                                    new DependencyProviderSetting
                                                        {
                                                            Name = DependencyProviderValidSettingName.DeleteArchiveFiles,
                                                            Value = "False"
                                                        }
                                                }
                            }
                    });

            var actual = target.GetDownloader(componentType);
            Assert.AreNotEqual(actual, null);
            Assert.AreEqual(actual.DownloadType, "Downloader_ZippedDependency");
        }

        // Downloader_ZippedDependency based on the ComponentType "SourceControlCopy", "BinaryRepository" value requires infrastructure.
        #endregion

        /// <summary>
        /// GetDownloader should generate a DependencyManagementFoundationPluginNotFoundException in case of unknown downloader name.
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(DependencyManagementFoundationPluginNotFoundException))]
        public void GetDownloaderUnknownPluginStringTest()
        {
            var target = new DownloaderFactory();
            var emptyDownloaderName = string.Empty;

            // Unknown downloader name should cause DependencyManagementFoundationPluginNotFoundException
            target.GetDownloader(emptyDownloaderName);
        }
    }
}
