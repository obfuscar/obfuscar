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
		bool markedOnly;
		bool renameProperties;
		bool renameEvents;
		bool reuseNames;
		bool xmlMapping;
		bool hideStrings;
		string keyFile;

		public Settings( Variables vars )
		{
			inPath = vars.GetValue( "InPath", "." );
			outPath = vars.GetValue( "OutPath", "." );
			markedOnly = XmlConvert.ToBoolean( vars.GetValue( "MarkedOnly", "false" ) );

			renameProperties = XmlConvert.ToBoolean( vars.GetValue( "RenameProperties", "true" ) );
			renameEvents = XmlConvert.ToBoolean( vars.GetValue( "RenameEvents", "true" ) );
			reuseNames = XmlConvert.ToBoolean( vars.GetValue( "ReuseNames", "true" ) );
			hideStrings = XmlConvert.ToBoolean(vars.GetValue("HideStrings", "true"));

			xmlMapping = XmlConvert.ToBoolean( vars.GetValue( "XmlMapping", "false" ) );

			keyFile = vars.GetValue("KeyFile", null);
		}

		public string InPath
		{
			get { return inPath; }
		}

		public string OutPath
		{
			get { return outPath; }
		}

		public bool MarkedOnly
		{
			get { return markedOnly; }
		}

		public bool RenameProperties
		{
			get { return renameProperties; }
		}

		public bool RenameEvents
		{
			get { return renameEvents; }
		}

		public bool ReuseNames
		{
			get { return reuseNames; }
		}

		public bool HideStrings
		{
			get { return hideStrings; }
		}

		public bool XmlMapping
		{
			get { return xmlMapping; }
		}

		public string KeyFile
		{
			get { return keyFile; }
		}
	}
}
