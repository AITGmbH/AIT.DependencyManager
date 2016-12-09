using AIT.DMF.Contracts.Common;
using AIT.DMF.Contracts.Gui;
using AIT.DMF.Contracts.Services;
using AIT.DMF.DependencyService.Integration.Test.Config;
using AIT.DMF.DependencyService.Integration.Test.Helpers;
using AIT.DMF.DependencyService.Integration.Test.Preparation;
using AIT.DMF.Plugins.Resolver.BinaryRepository;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.IO;
using System.Linq;

namespace AIT.DMF.DependencyService.Integration.Test
{
    [TestClass]
    public class DependencyBinaryRepositoryTest
    {
        private string _localWorkspaceFolder = string.Empty;
        private string _workspaceName = string.Empty;

        /// <summary>
        /// In progress...
        /// </summary>
        [TestInitialize]
        public void Init()
        {
            var guid = Guid.NewGuid().ToString();
            _localWorkspaceFolder = Path.Combine(Environment.CurrentDirectory, $"{Values.WorkSpaceName}_{guid}");

            _workspaceName = $"{Values.WorkSpaceName}_{guid}";

            BinaryRepositoryPreparation.PrepareBinaryRepositoryEnvironment(Values.WorkspaceOwner, Values.Password, Values.TeamProjectCollection, _workspaceName, Values.TeamProjectName, _localWorkspaceFolder);
        }

        [TestMethod]
        [TestCategory("IntegrationTest")]
        public void BinaryRepository_Resolver_Success()
        {
            // Arrange:
            DependencyResolverFactory.RegisterResolverType(new BinaryRepositoryResolverType());

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
            service.Setup(f => f.GetSetting(ServiceValidSettings.BinaryRepositoryTeamProject)).Returns("BinaryRepository");
            service.Setup(f => f.GetSetting(ServiceValidSettings.DefaultDependencyDefinitionFilename)).Returns("component.targets");

            // Act:
            var result = target.GetDependencyGraph(service.Object, _localWorkspaceFolder + @"\BinaryRepo\component.targets");
            var dependencyService = new DependencyService(service.Object);
            dependencyService.DownloadGraph(result, logger.Object, true, true);

            // Assert:
            var dllFiles = Directory.GetFiles(_localWorkspaceFolder + @"\Bin", "*.txt").Select(path => Path.GetFileName(path)).ToArray();
            Assert.AreEqual("test1.txt", dllFiles.First());
        }

        [TestCleanup]
        public void Clean()
        {
            WorkspaceHelper.WorkspaceCleanup(Values.WorkspaceOwner, Values.Password, Values.TeamProjectCollection, _workspaceName, Values.WorkspaceOwner);

            if (Directory.Exists(_localWorkspaceFolder))
            {
                Directory.Delete(_localWorkspaceFolder, true);
            }
        }
    }
}
