using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AIT.DMF.DependencyService.Test
{
    /// <summary>
    ///This is a test class for ParserXmlTest and is intended
    ///to contain all ParserXmlTest Unit Tests
    ///</summary>
    [TestClass]
    public class XmlParserTest
    {
        private TestContext testContextInstance;

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext
        {
            get
            {
                return testContextInstance;
            }
            set
            {
                testContextInstance = value;
            }
        }



        /// <summary>
        ///A test for ReadDependencyFile
        ///</summary>
        [TestMethod()]
        public void ReadDependencyFileTest()
        {
            // Preparing XDocument object
            /*XDocument compTargetsXDoc = null;
            using (MemoryStream ms = new MemoryStream())
            {
                StreamWriter sw = new StreamWriter(ms);
                sw.WriteLine("<Component Version=\"1.0\" Name=\"Root_Comp\">");
                sw.WriteLine("<Dependencies>");
                sw.WriteLine("<Dependency Type=\"SourceDependency\">");
                sw.WriteLine("<Provider Type = \"SourceControl\">");
                sw.WriteLine("<Settings Type=\"SourceControlSettings\">");
                sw.WriteLine("<Setting Name=\"ServerRootPath\" Value=\"$/TestFramework/Main/Src\" />");
                sw.WriteLine("<Setting Name=\"VersionSpec\" Value=\"T\" />");
                sw.WriteLine("<Setting Name=\"LocalRootPath\" Value=\"C:\\DepMgmtTest\\TestFramework_ABCD_123\" />");
                sw.WriteLine("</Settings>");
                sw.WriteLine("</Provider>");
                sw.WriteLine("</Dependency>");
                sw.WriteLine("<Dependency Type=\"BinaryDependency\">");
                sw.WriteLine("<Provider Type = \"BuildResult\">");
                sw.WriteLine("<Settings Type=\"BuildResultSettings\">");
                sw.WriteLine("<Setting Name=\"TeamProjectName\" Value=\"TestFramework\" />");
                sw.WriteLine("<Setting Name=\"BuildDefinition\" Value=\"ABCD\" />");
                sw.WriteLine("<Setting Name=\"BuildNumber\" Value=\"123\" />");
                sw.WriteLine("<Setting Name=\"LocalRootPath\" Value=\"C:\\DepMgmtTest\\TestFramework_ABCD_123\" />");
                sw.WriteLine("</Settings>");
                sw.WriteLine("</Provider>");
                sw.WriteLine("</Dependency>");
                sw.WriteLine("<Dependency Type=\"BinaryDependency\">");
                sw.WriteLine("<Provider Type = \"FileShare\">");
                sw.WriteLine("<Settings Type=\"FileShareSettings\">");
                sw.WriteLine("<Setting Name=\"FileShareRootPath\" Value=\"\\\\localhost\\\\Dependency 3\\Version 1.0.0.35\\\" />");
                sw.WriteLine("<Setting Name=\"LocalRootPath\" Value=\"C:\\DepMgmtTest\\Dep3_1.0.0.35\" />");
                sw.WriteLine("</Settings>");
                sw.WriteLine("</Provider>");
                sw.WriteLine("</Dependency>");
                sw.WriteLine("</Dependencies>");
                sw.WriteLine("</Component>");
                sw.Flush();
                sw.BaseStream.Seek(0, SeekOrigin.Begin);

                compTargetsXDoc = XDocument.Load(ms);
                sw.Close();
            }

            ParserXml target = new ParserXml();
            XMLComponent actual = (XMLComponent) target.ReadDependencyFile(compTargetsXDoc);

            // Test XMLComponent
            Assert.AreEqual(actual.Name, "Root_Comp");
            Assert.AreEqual(actual.Version, "1.0");
            Assert.AreEqual(actual.Dependencies.Count, 3);
            // Test XMLDependency
            // TODO
            */
        }
    }
}
