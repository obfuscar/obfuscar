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
using System.Xml;

namespace Obfuscar
{
    class Settings
    {
        internal const string VariableAnalyzeXaml = "AnalyzeXaml";
        internal const string VariableCustomChars = "CustomChars";
        internal const string VariableExtraFrameworkFolders = "ExtraFrameworkFolders";
        internal const string VariableHidePrivateApi = "HidePrivateApi";
        internal const string VariableHideStrings = "HideStrings";
        internal const string VariableInPath = "InPath";
        internal const string VariableKeepPublicApi = "KeepPublicApi";
        internal const string VariableKeyContainer = "KeyContainer";
        internal const string VariableKeyFile = "KeyFile";
        internal const string VariableLogFile = "LogFile";
        internal const string VariableMarkedOnly = "MarkedOnly";
        internal const string VariableOptimizeMethods = "OptimizeMethods";
        internal const string VariableOutPath = "OutPath";
        internal const string VariableRegenerateDebugInfo = "RegenerateDebugInfo";
        internal const string VariableRenameEvents = "RenameEvents";
        internal const string VariableRenameFields = "RenameFields";
        internal const string VariableRenameProperties = "RenameProperties";
        internal const string VariableReuseNames = "ReuseNames";
        internal const string VariableSuppressIldasm = "SuppressIldasm";
        internal const string VariableUseKoreanNames = "UseKoreanNames";
        internal const string VariableUseUnicodeNames = "UseUnicodeNames";
        internal const string VariableXmlMapping = "XmlMapping";

        internal const string SpecialVariableProjectFileDirectory = "ProjectFileDirectory";

        public Settings(Variables vars)
        {
            InPath = Environment.ExpandEnvironmentVariables(vars.GetValue(VariableInPath, "."));
            OutPath = Environment.ExpandEnvironmentVariables(vars.GetValue(VariableOutPath, "."));
            LogFilePath = Environment.ExpandEnvironmentVariables(vars.GetValue(VariableLogFile, ""));
            MarkedOnly = XmlConvert.ToBoolean(vars.GetValue(VariableMarkedOnly, "false"));

            RenameFields = XmlConvert.ToBoolean(vars.GetValue(VariableRenameFields, "true"));
            RenameProperties = XmlConvert.ToBoolean(vars.GetValue(VariableRenameProperties, "true"));
            RenameEvents = XmlConvert.ToBoolean(vars.GetValue(VariableRenameEvents, "true"));
            KeepPublicApi = XmlConvert.ToBoolean(vars.GetValue(VariableKeepPublicApi, "true"));
            HidePrivateApi = XmlConvert.ToBoolean(vars.GetValue(VariableHidePrivateApi, "true"));
            ReuseNames = XmlConvert.ToBoolean(vars.GetValue(VariableReuseNames, "true"));
            UseUnicodeNames = XmlConvert.ToBoolean(vars.GetValue(VariableUseUnicodeNames, "false"));
            UseKoreanNames = XmlConvert.ToBoolean(vars.GetValue(VariableUseKoreanNames, "false"));
            HideStrings = XmlConvert.ToBoolean(vars.GetValue(VariableHideStrings, "true"));
            Optimize = XmlConvert.ToBoolean(vars.GetValue(VariableOptimizeMethods, "true"));
            SuppressIldasm = XmlConvert.ToBoolean(vars.GetValue(VariableSuppressIldasm, "true"));

            XmlMapping = XmlConvert.ToBoolean(vars.GetValue(VariableXmlMapping, "false"));
            RegenerateDebugInfo = XmlConvert.ToBoolean(vars.GetValue(VariableRegenerateDebugInfo, "false"));
            AnalyzeXaml = XmlConvert.ToBoolean(vars.GetValue(VariableAnalyzeXaml, "false"));
            CustomChars = vars.GetValue(VariableCustomChars, "");
        }

        public bool RegenerateDebugInfo { get; }

        public string InPath { get; }

        public string OutPath { get; }

        public bool MarkedOnly { get; }

        public string LogFilePath { get; }

        public bool RenameFields { get; }

        public bool RenameProperties { get; }

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

        public string CustomChars { get; }
    }
}
