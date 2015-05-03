﻿using System.IO;
using Calamari.Integration.FileSystem;
using Calamari.Tests.Helpers;
using NUnit.Framework;
using Octostache;

namespace Calamari.Tests.Fixtures.ScriptCS
{
    [TestFixture]
    [Category(TestEnvironment.CompatableOS.All)]
    public class ScriptCSFixture : CalamariFixture
    {
        [Test, RequiresDotNet45]
        public void ShouldPrintEncodedVariable()
        {
            var output = Invoke(Calamari()
                .Action("run-script")
                .Argument("script", GetFixtureResouce("Scripts", "PrintEncodedVariable.csx")));

            output.AssertZero();
            output.AssertOutput("##octopus[setVariable name='RG9ua3k=' value='S29uZw==']");
        }

        [Test, RequiresDotNet45]
        public void ShouldCallHello()
        {
            var variablesFile = Path.GetTempFileName();

            var variables = new VariableDictionary();
            variables.Set("Name", "Paul");
            variables.Set("Variable2", "DEF");
            variables.Set("Variable3", "GHI");
            variables.Set("Foo_bar", "Hello");
            variables.Set("Host", "Never");
            variables.Save(variablesFile);

            using (new TemporaryFile(variablesFile))
            {
                var output = Invoke(Calamari()
                    .Action("run-script")
                    .Argument("script", GetFixtureResouce("Scripts", "Hello.csx"))
                    .Argument("variables", variablesFile));

                output.AssertZero();
                output.AssertOutput("Hello Paul");
            }
        }


        readonly string FixtureDirectory = TestEnvironment.GetTestPath("Fixtures", "ScriptCS");
        private string GetFixtureResouce(params string[] paths)
        {
            return Path.Combine(FixtureDirectory, Path.Combine(paths));
        }
    }
}