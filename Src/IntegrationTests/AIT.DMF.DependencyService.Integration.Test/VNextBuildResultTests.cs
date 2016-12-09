using Moq;
using AIT.DMF.Contracts.Services;
using AIT.DMF.Contracts.Common;
using AIT.DMF.Contracts.Gui;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Linq;
using AIT.DMF.DependencyService.Integration.Test.Preparation;
using AIT.DMF.DependencyService.Integration.Test.Config;
using AIT.DMF.Plugins.Resolver.VNextBuildResult;

namespace AIT.DMF.DependencyService.Integration.Test
{
    [Ignore]
    [TestClass]
    public class VNextBuildResultTests
    {
        /// <summary>
        /// In progress ...
        /// </summary>
        [TestInitialize]
        public void Init()
        {
            // 1- Add project that you want to build ??
            // 2- Add target file
            var xamlBuildData = Path.GetFullPath("TestData");
            xamlBuildData = xamlBuildData.Replace('\\', '/');
            xamlBuildData = xamlBuildData.Replace("/bin/Debug/TestData", "");
            VNextBuildResultPreparation.PrepareVNextBuildResultEnvironment(Values.WorkspaceOwner, Values.Password, Values.TeamProjectCollection, Values.WorkSpaceName, xamlBuildData, Values.LocalRootDisk);
            // 3- Copy build definition to destination folder
        }

        [TestMethod]
        public void VNextBuildResult_Resolver_Success()
        {
            // Arrange:
            DependencyResolverFactory.RegisterResolverType(new VNextBuildResultResolverType());

            // Mock logger:
            Mock<ILogger> logger = new Mock<ILogger>();
            logger.Setup(f => f.LogMsg(It.IsAny<string>()));
            logger.Setup(f => f.ShowMessages());

            // Mock dependency graph:
            DependencyGraphCreator target = new DependencyGraphCreator("Data", logger.Object, true);
            Mock<ISettings<ServiceValidSettings>> service = new Mock<ISettings<ServiceValidSettings>>();
            service.Setup(f => f.GetSetting(ServiceValidSettings.DefaultTeamProjectCollection)).Returns(Values.TeamProjectCollection);
            service.Setup(f => f.GetSetting(ServiceValidSettings.DefaultWorkspaceName)).Returns(Values.WorkSpaceName);
            service.Setup(f => f.GetSetting(ServiceValidSettings.DefaultWorkspaceOwner)).Returns(Values.WorkspaceOwner);
            service.Setup(f => f.GetSetting(ServiceValidSettings.DefaultOutputBaseFolder)).Returns(Values.PathToTeamProject);
            service.Setup(f => f.GetSetting(ServiceValidSettings.BinaryTeamProjectCollectionUrl)).Returns(Values.TeamProjectCollection);
            service.Setup(f => f.GetSetting(ServiceValidSettings.DefaultRelativeOutputPath)).Returns(@"..\Bin");
            service.Setup(f => f.GetSetting(ServiceValidSettings.BinaryRepositoryTeamProject)).Returns("");
            service.Setup(f => f.GetSetting(ServiceValidSettings.DefaultDependencyDefinitionFilename)).Returns("component.targets");

            // Act:
            var result = target.GetDependencyGraph(service.Object, Values.PathToTeamProject + @"\VNextBuildResult\component.targets");
            var dependencyService = new DependencyService(service.Object);
            dependencyService.DownloadGraph(result, logger.Object, true, true);

            // Assert:
            var dllFiles = Directory.GetFiles(Values.DependencyOutputPath, "*").Select(path => Path.GetFileName(path)).ToArray();
            Assert.AreEqual("AssemblyInfo.cs", dllFiles.First());
        }

        [TestCleanup]
        public void Clean()
        {
            Directory.Delete(Values.DependencyOutputPath, true);
        }
    }
}
