﻿using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Calamari.Deployment;
using Calamari.Integration.FileSystem;
using Calamari.Integration.Substitutions;
using Calamari.Tests.Helpers;
using NUnit.Framework;
using Octostache;

namespace Calamari.Tests.Fixtures.Substitutions
{
    [TestFixture]
    public class SubstitutionsFixture : CalamariFixture
    {
        static readonly WindowsPhysicalFileSystem FileSystem = new WindowsPhysicalFileSystem();

        [Test]
        public void ShouldSubstitute()
        {
            var variables = new VariableDictionary();
            variables["ServerEndpoints[FOREXUAT01].Name"] = "forexuat01.local";
            variables["ServerEndpoints[FOREXUAT01].Port"] = "1566";
            variables["ServerEndpoints[FOREXUAT02].Name"] = "forexuat02.local";
            variables["ServerEndpoints[FOREXUAT02].Port"] = "1566";
            
            var text = PerformTest(GetFixtureResouce("Samples","Servers.json"), variables).Text;

            Assert.That(Regex.Replace(text, "\\s+", ""), Is.EqualTo(@"{""Servers"":[{""Name"":""forexuat01.local"",""Port"":1566},{""Name"":""forexuat02.local"",""Port"":1566}]}"));
        }

        [Test]
        public void ShouldRetainEncodingIfNoneSet()
        {
            var filePath = GetFixtureResouce("Samples", "UTF16LE.ini");
            var variables = new VariableDictionary();
            variables["LocalCacheFolderName"] = "SpongeBob";

            var result = PerformTest(filePath, variables);
            
            Assert.AreEqual(Encoding.Unicode, FileSystem.GetFileEncoding(filePath));
            Assert.AreEqual(Encoding.Unicode, result.Encoding);
            Assert.True(Regex.Match(result.Text, "\\bLocalCacheFolderName=SpongeBob\\b").Success);
        }

        [Test]
        public void ShouldOverrideEncodingIfProvided()
        {

            var filePath = GetFixtureResouce("Samples", "UTF16LE.ini");
            var variables = new VariableDictionary();
            variables[SpecialVariables.Package.SubstituteInFilesOutputEncoding] = "utf-8";

            var encoding = (Encoding)PerformTest(filePath, variables).Encoding;
            
            Assert.AreEqual(Encoding.Unicode, FileSystem.GetFileEncoding(filePath));
            Assert.AreEqual(Encoding.UTF8, encoding);
        }

        [Test]
        public void ShouldRevertToExistingEncodingIfInvalid()
        {

            var filePath = GetFixtureResouce("Samples", "UTF16LE.ini");
            var variables = new VariableDictionary();
            variables[SpecialVariables.Package.SubstituteInFilesOutputEncoding] = "utf-666";

            var encoding = (Encoding)PerformTest(filePath, variables).Encoding;

            Assert.AreEqual(Encoding.Unicode, FileSystem.GetFileEncoding(filePath));
            Assert.AreEqual(Encoding.Unicode, encoding);
        }

        dynamic PerformTest(string sampleFile, VariableDictionary variables)
        {
            var temp = Path.GetTempFileName();
            using (new TemporaryFile(temp))
            {
                var substituter = new FileSubstituter(FileSystem);
                substituter.PerformSubstitution(sampleFile, variables, temp);
                return new {
                    Text = FileSystem.ReadFile(temp),
                    Encoding = FileSystem.GetFileEncoding(temp)
                };
            }
        }
    }
}
