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
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace Obfuscar
{
	class Settings
	{
		string inPath;
		string outPath;
		string logFilePath;
		bool markedOnly;
		bool renameProperties;
		bool renameEvents;
		bool keepPublicApi;
		bool hidePrivateApi;
		bool reuseNames;
		bool useUnicodeNames;
		bool useKoreanNames;
		bool xmlMapping;
		bool hideStrings;
		bool renegerateDebugInfo;
		string keyFile;

		public Settings (Variables vars)
		{
			inPath = vars.GetValue ("InPath", ".");
			outPath = vars.GetValue ("OutPath", ".");
			logFilePath = vars.GetValue ("LogFile", "");
			markedOnly = XmlConvert.ToBoolean (vars.GetValue ("MarkedOnly", "false"));

			renameProperties = XmlConvert.ToBoolean (vars.GetValue ("RenameProperties", "true"));
			renameEvents = XmlConvert.ToBoolean (vars.GetValue ("RenameEvents", "true"));
			keepPublicApi = XmlConvert.ToBoolean (vars.GetValue ("KeepPublicApi", "false"));
			hidePrivateApi = XmlConvert.ToBoolean (vars.GetValue ("HidePrivateApi", "false"));
			reuseNames = XmlConvert.ToBoolean (vars.GetValue ("ReuseNames", "true"));
			useUnicodeNames = XmlConvert.ToBoolean (vars.GetValue ("UseUnicodeNames", "false"));
			useKoreanNames = XmlConvert.ToBoolean (vars.GetValue ("UseKoreanNames", "false"));
			hideStrings = XmlConvert.ToBoolean (vars.GetValue ("HideStrings", "true"));

			xmlMapping = XmlConvert.ToBoolean (vars.GetValue ("XmlMapping", "false"));
			renegerateDebugInfo = XmlConvert.ToBoolean (vars.GetValue ("RegenerateDebugInfo", "false"));
			keyFile = vars.GetValue ("KeyFile", null);
		}

		public bool RegenerateDebugInfo {
			get { return renegerateDebugInfo; }
		}

		public string InPath {
			get { return inPath; }
		}

		public string OutPath {
			get { return outPath; }
		}

		public bool MarkedOnly {
			get { return markedOnly; }
		}

		public string LogFilePath {
			get { return logFilePath; }
		}

		public bool RenameProperties {
			get { return renameProperties; }
		}

		public bool RenameEvents {
			get { return renameEvents; }
		}

		public bool KeepPublicApi {
			get { return keepPublicApi; }			
		}

		public bool HidePrivateApi {
			get { return hidePrivateApi; }
		}

		public bool ReuseNames {
			get { return reuseNames; }
		}

		public bool HideStrings {
			get { return hideStrings; }
		}

		public bool XmlMapping {
			get { return xmlMapping; }
		}

		public bool UseUnicodeNames {
			get { return useUnicodeNames; }
		}

		public bool UseKoreanNames {
			get { return useKoreanNames; }
		}

		public string KeyFile {
			get { return keyFile; }
		}
	}
}
