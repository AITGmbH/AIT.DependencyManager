using Moq;
using AIT.DMF.Contracts.Services;
using AIT.DMF.Contracts.Common;
using AIT.DMF.Contracts.Gui;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Linq;
using AIT.DMF.DependencyService.Integration.Test.Preparation;
using AIT.DMF.DependencyService.Integration.Test.Config;
using AIT.DMF.Plugins.Resolver.Subversion;
using System;
using System.IO.Compression;
using AIT.DMF.DependencyService.Integration.Test.Helpers;

namespace AIT.DMF.DependencyService.Integration.Test
{
    [TestClass]
    public class DependencySubversionTest
    {
        string _localWorkspaceFolder = string.Empty;
        string _workspaceName = string.Empty;

        /// <summary>
        /// In progress...
        /// </summary>
        [TestInitialize]
        public void Init()
        {
            var guid = System.Guid.NewGuid().ToString();
            _localWorkspaceFolder = Path.Combine(Environment.CurrentDirectory, $"{Values.WorkSpaceName}_{guid}");

            _workspaceName = $"{Values.WorkSpaceName}_{guid}";

            var resourcesDir = Path.GetFullPath("Resources");
            var workingDir = Path.Combine(_localWorkspaceFolder, "TestData");

            //Copy existing SVN Repository and extract it
            Directory.CreateDirectory(workingDir);
            File.Copy(Path.Combine(resourcesDir, Values.SvnRepoFile), Path.Combine(workingDir, Values.SvnRepoFile));
            ZipFile.ExtractToDirectory(Path.Combine(workingDir, Values.SvnRepoFile), workingDir);

            var subversionPath = workingDir.Replace('\\', '/');

            SubversionPreparation.PrepareSubVersionTestEnvironment(Values.WorkspaceOwner, Values.Password, Values.TeamProjectCollection, _workspaceName, Values.TeamProjectName, _localWorkspaceFolder, subversionPath);
        }

        [TestMethod]
        [TestCategory("IntegrationTest")]
        public void Subversion_Resolver_Success()
        {
            // Arrange:
            DependencyResolverFactory.RegisterResolverType(new SubversionResolverType());

            // Mock logger:
            Mock<ILogger> logger = new Mock<ILogger>();
            logger.Setup(f => f.LogMsg(It.IsAny<string>()));
            logger.Setup(f => f.ShowMessages());

            // Mock dependency graph:
            DependencyGraphCreator target = new DependencyGraphCreator("Data", logger.Object, true);
            Mock<ISettings<ServiceValidSettings>> service = new Mock<ISettings<ServiceValidSettings>>();
            service.Setup(f => f.GetSetting(ServiceValidSettings.DefaultTeamProjectCollection)).Returns(Values.TeamProjectCollection);
            service.Setup(f => f.GetSetting(ServiceValidSettings.DefaultWorkspaceName)).Returns(_workspaceName);
            service.Setup(f => f.GetSetting(ServiceValidSettings.DefaultWorkspaceOwner)).Returns(Values.WorkspaceOwner);
            service.Setup(f => f.GetSetting(ServiceValidSettings.DefaultOutputBaseFolder)).Returns(_localWorkspaceFolder);
            service.Setup(f => f.GetSetting(ServiceValidSettings.BinaryTeamProjectCollectionUrl)).Returns(Values.TeamProjectCollection);
            service.Setup(f => f.GetSetting(ServiceValidSettings.DefaultRelativeOutputPath)).Returns(@".\Bin");
            service.Setup(f => f.GetSetting(ServiceValidSettings.BinaryRepositoryTeamProject)).Returns("");
            service.Setup(f => f.GetSetting(ServiceValidSettings.DefaultDependencyDefinitionFilename)).Returns("component.targets");

            // Act:
            var result = target.GetDependencyGraph(service.Object, _localWorkspaceFolder + @"\Subversion\component.targets");
            var dependencyService = new DependencyService(service.Object);
            dependencyService.DownloadGraph(result, logger.Object, true, true);

            // Assert:
            var dllFiles = Directory.GetFiles(_localWorkspaceFolder + @"\Bin", "*.dll").Select(path => Path.GetFileName(path)).ToArray();
            Assert.AreEqual("ComponentA.1_0.dll", dllFiles.First());
        }

        [TestCleanup]
        public void Clean()
        {
            WorkspaceHelper.WorkspaceCleanup(Values.WorkspaceOwner, Values.Password, Values.TeamProjectCollection, _workspaceName, Values.WorkspaceOwner);

            Directory.Delete(_localWorkspaceFolder, true);
        }
    }
}
