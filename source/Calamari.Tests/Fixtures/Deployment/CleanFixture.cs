﻿using System;
using System.Globalization;
using System.IO;
using System.Xml.Linq;
using Calamari.Deployment.Journal;
using Calamari.Integration.FileSystem;
using Calamari.Integration.Processes;
using Calamari.Tests.Helpers;
using NUnit.Framework;
using Octostache;

namespace Calamari.Tests.Fixtures.Deployment
{
    [TestFixture]
    [Category(TestEnvironment.CompatableOS.All)]
    public class CleanFixture : CalamariFixture
    {
        CalamariResult result;
        ICalamariFileSystem fileSystem;
        IDeploymentJournal deploymentJournal;
        VariableDictionary variables;
        string tentacleDirectory;
        string packagesDirectory;
        string stagingDirectory;

        [SetUp]
        public void SetUp()
        {
            fileSystem = CalamariPhysicalFileSystem.GetPhysicalFileSystem();

            // Ensure tenticle directory exists
            tentacleDirectory = Path.Combine(Path.GetTempPath(), "CalamariTestTentacle");
            var tentacleHiddenDirectory = Path.Combine(tentacleDirectory, ".tentacle"); 
            fileSystem.EnsureDirectoryExists(tentacleDirectory);
            fileSystem.EnsureDirectoryExists(tentacleHiddenDirectory);
            fileSystem.PurgeDirectory(tentacleHiddenDirectory, DeletionOptions.TryThreeTimes);

            Environment.SetEnvironmentVariable("TentacleJournal", Path.Combine(tentacleHiddenDirectory, "DeploymentJournal.xml" ));

            variables = new VariableDictionary();
            variables.EnrichWithEnvironmentVariables();

            deploymentJournal = new DeploymentJournal(fileSystem, new SystemSemaphore(), variables);

            packagesDirectory = Path.Combine(Path.GetTempPath(), "CalamariTestPackages");
            fileSystem.EnsureDirectoryExists(packagesDirectory);
            stagingDirectory = Path.Combine(Path.GetTempPath(), "CalamariTestStaging");
            fileSystem.EnsureDirectoryExists(stagingDirectory);

            // Create some artificats
            const string retentionPolicySet1 = "retentionPolicySet1";

            CreateDeployment(Path.Combine(packagesDirectory, "Acme.1.0.0.nupkg"), Path.Combine(stagingDirectory, "Acme.1.0.0"), 
                new DateTimeOffset(new DateTime(2015, 01, 26), new TimeSpan(10, 0,0)), retentionPolicySet1);

            CreateDeployment(Path.Combine(packagesDirectory, "Acme.1.1.0.nupkg"), Path.Combine(stagingDirectory, "Acme.1.1.0"), 
                new DateTimeOffset(new DateTime(2015, 02, 01), new TimeSpan(10, 0,0)), retentionPolicySet1);

            CreateDeployment(Path.Combine(packagesDirectory, "Acme.1.2.0.nupkg"), Path.Combine(stagingDirectory, "Acme.1.2.0"), 
                new DateTimeOffset(new DateTime(2015, 02, 10), new TimeSpan(10, 0,0)), retentionPolicySet1);
        }

        [Test]
        public void ShouldRemoveArtifactsWhenDaysSpecified()
        {
            result = Clean("retentionPolicySet1", 3, null);

            result.AssertZero();

            Assert.False(fileSystem.DirectoryExists(Path.Combine(stagingDirectory, "Acme.1.0.0")));
            Assert.False(fileSystem.FileExists(Path.Combine(packagesDirectory, "Acme.1.0.0.nupkg")));
        }

        [Test]
        public void ShouldRemoveArtifactsWhenReleasesSpecified()
        {
            result = Clean("retentionPolicySet1", null, 1);

            result.AssertZero();

            Assert.False(fileSystem.DirectoryExists(Path.Combine(stagingDirectory, "Acme.1.0.0")));
            Assert.False(fileSystem.FileExists(Path.Combine(packagesDirectory, "Acme.1.0.0.nupkg")));
        }

        private void CreateDeployment(string extractedFrom, string extractedTo, DateTimeOffset date, string retentionPolicySet)
        {
            fileSystem.EnsureDirectoryExists(extractedTo);
            fileSystem.OverwriteFile(Path.Combine(extractedTo, "an_artifact.txt"), "lorem ipsum");
            fileSystem.OverwriteFile(extractedFrom, "lorem ipsum");

            deploymentJournal.AddJournalEntry(new JournalEntry(new XElement("Deployment", 
                new XAttribute("Id", Guid.NewGuid().ToString()),
                new XAttribute("EnvironmentId", "blah"),
                new XAttribute("ProjectId", "blah"),
                new XAttribute("PackageId", "blah"),
                new XAttribute("PackageVersion", "blah"),
                new XAttribute("InstalledOn", date.UtcDateTime.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture )),
                new XAttribute("ExtractedFrom", extractedFrom),
                new XAttribute("ExtractedTo", extractedTo),
                new XAttribute("RetentionPolicySet", retentionPolicySet),
                new XAttribute("WasSuccessFul", true.ToString())
                )));
        }


        CalamariResult Clean(string retentionPolicySet, int? days, int? releases)
        {
            return Invoke(Calamari()
                .Action("clean")
                .Argument("retentionPolicySet", retentionPolicySet)
                .Argument(days.HasValue ? "days" : "releases", days.HasValue ? days.ToString() : releases.ToString()));
        }
    }
}