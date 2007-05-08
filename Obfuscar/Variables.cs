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

namespace Obfuscar
{
	class Variables
	{
		readonly Dictionary<string, string> vars = new Dictionary<string, string>( );
		readonly System.Text.RegularExpressions.Regex re =
			new System.Text.RegularExpressions.Regex( @"\$\(([^)]+)\)" );

		public void Add( string name, string value )
		{
			vars[name] = value;
		}

		public void Remove( string name )
		{
			vars.Remove( name );
		}

		public string GetValue( string name, string def )
		{
			string value;
			if ( vars.TryGetValue( name, out value ) )
				return value;
			else
				return def;
		}

		public string Replace( string str )
		{
			System.Text.StringBuilder formatted = new System.Text.StringBuilder( );

			int lastMatch = 0;

			string variable;
			string replacement;
			foreach ( System.Text.RegularExpressions.Match m in re.Matches( str ) )
			{

				formatted.Append( str.Substring( lastMatch, m.Index - lastMatch ) );

				variable = m.Groups[1].Value;
				if ( vars.TryGetValue( variable, out replacement ) )
					formatted.Append( replacement );
				else
					throw new ApplicationException( "Unable to replace variable:  " + variable );

				lastMatch = m.Index + m.Length;
			}

			formatted.Append( str.Substring( lastMatch ) );

			return formatted.ToString( );
		}
	}
}
