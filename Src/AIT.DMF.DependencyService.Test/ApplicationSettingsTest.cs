using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AIT.DMF.DependencyService.Test
{
    [TestClass]
    public class ApplicationSettingsTest
    {
        private const string _testValue = "SomeRandomValue";

        [ClassCleanup()]
        public static void Cleanup()
        {
            ApplicationSettings.Instance.DeleteAllValues();
        }

        [TestMethod]
        public void GetRegistryKeyForSevenZipDllTest_ShouldReturnNull()
        {
            //Check that the path is null
            Assert.IsNull(ApplicationSettings.Instance.InstallPathForSevenZip);
        }

        [TestMethod]
        public void GetSingleton_ShouldReturnSingleton()
        {
            //Check that the path is null
            Assert.IsNotNull(ApplicationSettings.Instance);
            Assert.IsInstanceOfType(ApplicationSettings.Instance, ApplicationSettings.Instance.GetType());
        }

        [TestMethod]
        public void SetRegistryKeyForSevenZipDllTest_ShouldReturnTrue()
        {
            ApplicationSettings.Instance.InstallPathForSevenZip = _testValue;

            Assert.AreEqual(ApplicationSettings.Instance.InstallPathForSevenZip, _testValue);

            //Remove test key from Registry
            ApplicationSettings.Instance.DeleteAllValues();
        }

        [TestMethod]
        public void DeleteRegistryKeyForSevenZipDllTest_ShouldReturnTrue()
        {
            ApplicationSettings.Instance.DeleteAllValues();

            Assert.IsNull(ApplicationSettings.Instance.InstallPathForSevenZip);
        }
    }
}
