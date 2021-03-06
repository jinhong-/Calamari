﻿using System;
using System.CodeDom.Compiler;
using System.Globalization;
using System.IO;
using System.Text;
using Calamari.Integration.Processes;
using Octostache;

namespace Calamari
{
    public class Log
    {
        static string stdOutMode;
        static readonly object Sync = new object();

        static Log()
        {
            StdOut = Console.Out;
            StdErr = Console.Error;
        }

        public static TextWriter StdOut { get; set; }
        public static TextWriter StdErr { get; set; }

        static void SetMode(string mode)
        {
            if (stdOutMode == mode) return;
            StdOut.WriteLine("##octopus[stdout-" + mode + "]");
            stdOutMode = mode;
        }

        public static void Verbose(string message)
        {
            lock (Sync)
            {
                SetMode("verbose");
                StdOut.WriteLine(message);
            }
        }

        public static void SetOutputVariable(string name, string value)
        {
            SetOutputVariable(name, value, null);
        }

        public static void SetOutputVariable(string name, string value, VariableDictionary variables)
        {
            Info($"##octopus[setVariable name=\"{ConvertServiceMessageValue(name)}\" value=\"{ConvertServiceMessageValue(value)}\"]");

            variables?.SetOutputVariable(name, value);
        }

        static string ConvertServiceMessageValue(string value)
        {
            return Convert.ToBase64String(Encoding.Default.GetBytes(value));
        }

        public static void VerboseFormat(string messageFormat, params object[] args)
        {
            Verbose(string.Format(messageFormat, args));
        }

        public static void Info(string message)
        {
            lock (Sync)
            {
                SetMode("default");
                StdOut.WriteLine(message);
            }
        }

        public static void Info(string messageFormat, params object[] args)
        {
            Info(String.Format(messageFormat, args));
        }

        public static void Warn(string message)
        {
            lock (Sync)
            {
                SetMode("warning");
                StdOut.WriteLine(message);
            }
        }

        public static void WarnFormat(string messageFormat, params object[] args)
        {
            Warn(String.Format(messageFormat, args));
        }

        public static void Error(string message)
        {
            lock (Sync)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                StdErr.WriteLine(message);
                Console.ResetColor();
            }
        }

        public static void ErrorFormat(string messageFormat, params object[] args)
        {
            Error(string.Format(messageFormat, args));
        }

        public static class ServiceMessages
        {
            public static string ConvertServiceMessageValue(string value)
            {
                return Convert.ToBase64String(Encoding.Default.GetBytes(value));
            }

            public static void PackageFound(string packageId, string packageVersion, string packageHash,
                string packageFullPath, bool exactMatchExists = false)
            {
                if (exactMatchExists)
                    Verbose("##octopus[calamari-found-package]");

                VerboseFormat("##octopus[foundPackage id=\"{0}\" version=\"{1}\" hash=\"{2}\" remotePath=\"{3}\"]",
                    ConvertServiceMessageValue(packageId),
                    ConvertServiceMessageValue(packageVersion),
                    ConvertServiceMessageValue(packageHash),
                    ConvertServiceMessageValue(packageFullPath));

            }

            public static void DeltaVerification(string remotePath, string hash, long size)
            {
                VerboseFormat("##octopus[deltaVerification remotePath=\"{0}\" hash=\"{1}\" size=\"{2}\"]",
                    ConvertServiceMessageValue(remotePath),
                    ConvertServiceMessageValue(hash),
                    ConvertServiceMessageValue(size.ToString(CultureInfo.InvariantCulture)));
            }
        }
    }
}