using System;
using System.IO;
using Ionic.Zip;

namespace Calamari.Integration.Packages
{
    public class ZipPackageExtractor : SimplePackageExtractor
    {
        public override string[] Extensions { get { return new [] { ".zip"}; } }

        public override int Extract(string packageFile, string directory, bool suppressNestedScriptWarning)
        {
            var filesExtracted = 0;
            using (var inStream = new FileStream(packageFile, FileMode.Open, FileAccess.Read))
            using (ZipFile zip = ZipFile.Read(inStream))
            {
                foreach(var zipEntry in zip)
                {
                    ProcessEvent(ref filesExtracted, zipEntry, suppressNestedScriptWarning);
                    zipEntry.Extract(directory, ExtractExistingFileAction.OverwriteSilently);
                }
            }
            return filesExtracted;
        }

        protected void ProcessEvent(ref int filesExtracted, ZipEntry entry, bool suppressNestedScriptWarning)
        {
            if (entry.IsDirectory) return;

            filesExtracted++;

            if (!suppressNestedScriptWarning)
            {
                WarnIfScriptInSubFolder(entry.FileName);
            }
        }

        void WarnIfScriptInSubFolder(string path)
        {
            var fileName = Path.GetFileName(path);
            if (string.Equals(fileName, "Deploy.ps1", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(fileName, "PreDeploy.ps1", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(fileName, "PostDeploy.ps1", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(fileName, "DeployFailed.ps1", StringComparison.OrdinalIgnoreCase))
            {
                var directoryName = Path.GetDirectoryName(path);
                if (!string.IsNullOrWhiteSpace(directoryName))
                {
                    Log.WarnFormat("The script file \"{0}\" contained within the package will not be executed because it is contained within a child folder. As of Octopus Deploy 2.4, scripts in sub folders will not be executed.", path);
                }
            }
        }

    }
}