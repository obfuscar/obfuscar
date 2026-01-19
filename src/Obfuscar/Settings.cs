#region Copyright (c) 2007 Ryan Williams <drcforbin@gmail.com>

/// <copyright>
/// Copyright (c) 2007 Ryan Williams <drcforbin@gmail.com>
/// 
/// Permission is hereby granted, free of charge, to any person obtaining a copy
/// of this software and associated documentation files (the "Software"), to deal
/// in the Software without restriction, including without limitation the rights
/// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
/// copies of the Software, and to permit persons to whom the Software is
/// furnished to do so, subject to the following conditions:
/// 
/// The above copyright notice and this permission notice shall be included in
/// all copies or substantial portions of the Software.
/// 
/// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
/// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
/// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
/// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
/// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
/// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
/// THE SOFTWARE.
/// </copyright>

#endregion

using System;
using System.IO;
using System.Xml;

namespace Obfuscar
{
    class Settings
    {
        public Settings(Variables vars)
        {
            // Read raw config values (no environment variable expansion allowed).
            var inPathRaw = vars.GetValue("InPath", Environment.CurrentDirectory);
            var outPathRaw = vars.GetValue("OutPath", Environment.CurrentDirectory);
            var logFileRaw = vars.GetValue("LogFile", "");

            // Require absolute paths for internal use.
            if (!Path.IsPathRooted(inPathRaw))
                throw new ObfuscarException($"InPath must be an absolute path: '{inPathRaw}'");
            if (!Path.IsPathRooted(outPathRaw))
                throw new ObfuscarException($"OutPath must be an absolute path: '{outPathRaw}'");

            InPath = Path.GetFullPath(inPathRaw);
            OutPath = Path.GetFullPath(outPathRaw);

            if (!string.IsNullOrEmpty(logFileRaw))
            {
                if (!Path.IsPathRooted(logFileRaw))
                    throw new ObfuscarException($"LogFile must be an absolute path: '{logFileRaw}'");
                LogFilePath = Path.GetFullPath(logFileRaw);
            }
            else
            {
                LogFilePath = string.Empty;
            }
            MarkedOnly = XmlConvert.ToBoolean(vars.GetValue("MarkedOnly", "false"));

            RenameFields = XmlConvert.ToBoolean(vars.GetValue("RenameFields", "true"));
            RenameProperties = XmlConvert.ToBoolean(vars.GetValue("RenameProperties", "true"));
            KeepProperties = XmlConvert.ToBoolean(vars.GetValue("KeepProperties", "false"));
            RenameEvents = XmlConvert.ToBoolean(vars.GetValue("RenameEvents", "true"));
            KeepPublicApi = XmlConvert.ToBoolean(vars.GetValue("KeepPublicApi", "true"));
            HidePrivateApi = XmlConvert.ToBoolean(vars.GetValue("HidePrivateApi", "true"));
            ReuseNames = XmlConvert.ToBoolean(vars.GetValue("ReuseNames", "true"));
            UseUnicodeNames = XmlConvert.ToBoolean(vars.GetValue("UseUnicodeNames", "false"));
            UseKoreanNames = XmlConvert.ToBoolean(vars.GetValue("UseKoreanNames", "false"));
            HideStrings = XmlConvert.ToBoolean(vars.GetValue("HideStrings", "true"));
            Optimize = XmlConvert.ToBoolean(vars.GetValue("OptimizeMethods", "true"));
            SuppressIldasm = XmlConvert.ToBoolean(vars.GetValue("SuppressIldasm", "true"));

            XmlMapping = XmlConvert.ToBoolean(vars.GetValue("XmlMapping", "false"));
            RegenerateDebugInfo = XmlConvert.ToBoolean(vars.GetValue("RegenerateDebugInfo", "false"));
            AnalyzeXaml = XmlConvert.ToBoolean(vars.GetValue("AnalyzeXaml", "false"));
            SkipGenerated = XmlConvert.ToBoolean(vars.GetValue("SkipGenerated", "false"));
            CustomChars = vars.GetValue("CustomChars", "");
        }

        public bool RegenerateDebugInfo { get; }

        public string InPath { get; }

        public string OutPath { get; }

        public bool MarkedOnly { get; }

        public string LogFilePath { get; }

        public bool RenameFields { get; }

        public bool RenameProperties { get; }

        public bool KeepProperties { get; }

        public bool RenameEvents { get; }

        public bool KeepPublicApi { get; }

        public bool HidePrivateApi { get; }

        public bool ReuseNames { get; }

        public bool HideStrings { get; }

        public bool Optimize { get; }

        public bool SuppressIldasm { get; }

        public bool XmlMapping { get; }

        public bool UseUnicodeNames { get; }

        public bool UseKoreanNames { get; }

        public bool AnalyzeXaml { get; }

        public bool SkipGenerated { get; }

        public string CustomChars { get; }
    }
}
