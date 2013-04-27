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
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Collections.Generic;
using System.Text;

using Mono.Cecil;

namespace Obfuscar
{
	class AssemblyCache : IAssemblyResolver
	{
		private readonly Project project;
		private readonly Dictionary<string, AssemblyDefinition> cache =
			new Dictionary<string, AssemblyDefinition> ();
		private readonly IAssemblyResolver resolver = new V4AssemblyResolver ();
		private List<string> extraFolders = new List<string> ();

		public List<string> ExtraFolders { get { return extraFolders; } set { extraFolders = value; } }

		public AssemblyCache (Project project)
		{
			this.project = project;
		}

		private AssemblyDefinition SelfResolve (AssemblyNameReference name)
		{
			AssemblyDefinition assmDef;
			if (!cache.TryGetValue (name.FullName, out assmDef)) {
				assmDef = null;

				string[] exts = new string[] { ".dll", ".exe" };

				foreach (string ext in exts) {
					string file = Path.Combine (project.Settings.InPath, name.Name + ext);
					if (File.Exists (file) && MatchAssemblyName (file, name)) {
						assmDef = AssemblyDefinition.ReadAssembly (file);
						if (assmDef.Name.FullName != name.FullName) {
							assmDef = null;
							continue;
						}
						cache [name.FullName] = assmDef;
						break;
					}
				}
				if (assmDef == null) {
					foreach (string extrapath in extraFolders) {
						foreach (string ext in exts) {
							string file = Path.Combine (extrapath, name.Name + ext);
							if (File.Exists (file) && MatchAssemblyName (file, name)) {
								assmDef = AssemblyDefinition.ReadAssembly (file);
								if (assmDef.Name.FullName != name.FullName) {
									assmDef = null;
									continue;
								}
								cache [name.FullName] = assmDef;
								return assmDef;
							}
						}
					}
				}
			}

			return assmDef;
		}

		private bool MatchAssemblyName (string file, AssemblyNameReference name)
		{
			try {
				System.Reflection.AssemblyName an = System.Reflection.AssemblyName.GetAssemblyName (file);
				return (an.FullName == name.FullName);

			} catch {
				return true;
			}
		}

		public TypeDefinition GetTypeDefinition (TypeReference type)
		{
			if (type == null)
				return null;

			TypeDefinition typeDef = type as TypeDefinition;
			if (typeDef == null) {
				AssemblyNameReference name = type.Scope as AssemblyNameReference;
				if (name != null) {
					// try to self resolve, fall back to default resolver
					AssemblyDefinition assmDef = null;
					assmDef = SelfResolve (name);
					if (assmDef == null) {
						try {
							Console.WriteLine ("Trying to resolve dependency: " + name);
							assmDef = resolver.Resolve (name);
							cache [name.FullName] = assmDef;
						} catch (FileNotFoundException) {
							throw new ApplicationException ("Unable to resolve dependency:  " + name.Name);
						}
					}

					string fullName = null;
					while (type.IsNested) {
						if (fullName == null)
							fullName = type.Name;
						else
							fullName = type.Name + "/" + fullName;
						type = type.DeclaringType;
					}
					if (fullName == null)
						fullName = type.Namespace + "." + type.Name;
					else
						fullName = type.Namespace + "." + type.Name + "/" + fullName;
					typeDef = assmDef.MainModule.GetType (fullName);
				} else {
					GenericInstanceType gi = type as GenericInstanceType;
					if (gi != null)
						return GetTypeDefinition (gi.ElementType);
				}
			}

			return typeDef;
		}

		#region IAssemblyResolver Members

		public AssemblyDefinition Resolve (AssemblyNameReference name)
		{
			return SelfResolve (name);
		}

		public AssemblyDefinition Resolve (AssemblyNameReference name, ReaderParameters parameters)
		{
			return Resolve (name);
		}

		public AssemblyDefinition Resolve (string fullName)
		{
			AssemblyDefinition assmDef = null;
			cache.TryGetValue (fullName, out assmDef);
			return assmDef;
		}

		public AssemblyDefinition Resolve (string fullName, ReaderParameters parameters)
		{
			return Resolve (fullName);
		}

		#endregion
	}

	public delegate AssemblyDefinition AssemblyResolveEventHandler (object sender, AssemblyNameReference reference);

	public sealed class AssemblyResolveEventArgs : EventArgs
	{

		readonly AssemblyNameReference reference;

		public AssemblyNameReference AssemblyReference {
			get { return reference; }
		}

		public AssemblyResolveEventArgs (AssemblyNameReference reference)
		{
			this.reference = reference;
		}
	}

	public class V4AssemblyResolver : IAssemblyResolver
	{
		private List<String> m_directories;
		private string[] m_monoGacPaths;
		private static readonly string[] _extentions = new string[]
		{
			".dll", 
			".exe"
		};

		private string[] MonoGacPaths {
			get {
				if (this.m_monoGacPaths == null) {
					this.m_monoGacPaths = GetDefaultMonoGacPaths ().ToArray ();
				}
				return this.m_monoGacPaths;
			}
		}

		public void AddSearchDirectory (string directory)
		{
			this.m_directories.Add (directory);
		}

		public void RemoveSearchDirectory (string directory)
		{
			this.m_directories.Remove (directory);
		}

		public string[] GetSearchDirectories ()
		{
			return (string[])this.m_directories.ToArray ();
		}

		public AssemblyDefinition Resolve (AssemblyNameReference name, ReaderParameters parameters)
		{
			return Resolve (name);
		}

		public virtual AssemblyDefinition Resolve (string fullName)
		{
			return this.Resolve (AssemblyNameReference.Parse (fullName));
		}

		public AssemblyDefinition Resolve (string fullName, ReaderParameters parameters)
		{
			return Resolve (fullName);
		}

		public V4AssemblyResolver ()
		{
			this.m_directories = new List<String> ();
			this.m_directories.Add (".");
			this.m_directories.Add ("bin"); 
		}

		public event AssemblyResolveEventHandler ResolveFailure;

		public virtual AssemblyDefinition Resolve (AssemblyNameReference name)
		{
			if (name == null)
				throw new ArgumentNullException ("name");

			var assembly = SearchDirectory (name, m_directories);
			if (assembly != null)
				return assembly;

#if !SILVERLIGHT && !CF
			var framework_dir = Path.GetDirectoryName (typeof(object).Module.FullyQualifiedName);

			if (IsZero (name.Version)) {
				assembly = SearchDirectory (name, new[] { framework_dir });
				if (assembly != null)
					return assembly;
			}

			if (name.Name == "mscorlib") {
				assembly = GetCorlib (name);
				if (assembly != null)
					return assembly;
			}

			assembly = GetAssemblyInGac (name);
			if (assembly != null)
				return assembly;

			assembly = SearchDirectory (name, new[] { framework_dir });
			if (assembly != null)
				return assembly;
#endif

			if (ResolveFailure != null) {
				assembly = ResolveFailure (this, name);
				if (assembly != null)
					return assembly;
			}

			throw new FileNotFoundException (name.ToString ());
		}

		private static AssemblyDefinition SearchDirectory (AssemblyNameReference name, IEnumerable<String> directories)
		{
			AssemblyDefinition result;
			foreach (string dir in directories) {
				string[] extentions = _extentions;
				for (int i = 0; i < extentions.Length; i++) {
					string ext = extentions [i];
					string file = Path.Combine (dir, name.Name + ext);
					if (File.Exists (file)) {
						result = AssemblyDefinition.ReadAssembly (file);
						return result;
					}
				}
			}
			result = null;
			return result;
		}

		private static bool IsZero (Version version)
		{
			return version.Major == 0 && version.Minor == 0 && version.Build == 0 && version.Revision == 0;
		}

		AssemblyDefinition GetCorlib (AssemblyNameReference reference)
		{
			var version = reference.Version;
			var corlib = typeof(object).Assembly.GetName ();

			if (corlib.Version == version || IsZero (version))
				return GetAssembly (typeof(object).Module.FullyQualifiedName);

			var path = Directory.GetParent (
				Directory.GetParent (
					typeof(object).Module.FullyQualifiedName).FullName
			).FullName;

			if (OnMono ()) {
				if (version.Major == 1)
					path = Path.Combine (path, "1.0");
				else if (version.Major == 2) {
					if (version.MajorRevision == 5)
						path = Path.Combine (path, "2.1");
					else
						path = Path.Combine (path, "2.0");
				} else if (version.Major == 4)
					path = Path.Combine (path, "4.0");
				else
					throw new NotSupportedException ("Version not supported: " + version);
			} else {
				switch (version.Major) {
				case 1:
					if (version.MajorRevision == 3300)
						path = Path.Combine (path, "v1.0.3705");
					else
						path = Path.Combine (path, "v1.0.5000.0");
					break;
				case 2:
					path = Path.Combine (path, "v2.0.50727");
					break;
				case 4:
					path = Path.Combine (path, "v4.0.30319");
					break;
				default:
					throw new NotSupportedException ("Version not supported: " + version);
				}
			}

			var file = Path.Combine (path, "mscorlib.dll");
			if (File.Exists (file))
				return GetAssembly (file);

			return null;
		}

		AssemblyDefinition GetAssembly (string file)
		{
			return AssemblyDefinition.ReadAssembly (file);
		}

		public static bool OnMono ()
		{
			return typeof(object).Assembly.GetType ("System.MonoType", false) != null;
		}

		static List<string> GetGacPaths ()
		{
			if (OnMono ())
				return GetDefaultMonoGacPaths ();

			var paths = new List<string> (2);
			var windir = Environment.GetEnvironmentVariable ("WINDIR");
			if (windir == null)
				return paths;

			paths.Add (Path.Combine (windir, "assembly"));
			paths.Add (Path.Combine (windir, Path.Combine ("Microsoft.NET", "assembly")));
			return paths;
		}

		static List<string> GetDefaultMonoGacPaths ()
		{
			var paths = new List<string> (1);
			var gac = GetCurrentMonoGac ();
			if (gac != null)
				paths.Add (gac);

			var gac_paths_env = Environment.GetEnvironmentVariable ("MONO_GAC_PREFIX");
			if (string.IsNullOrEmpty (gac_paths_env))
				return paths;

			var prefixes = gac_paths_env.Split (Path.PathSeparator);
			foreach (var prefix in prefixes) {
				if (string.IsNullOrEmpty (prefix))
					continue;

				var gac_path = Path.Combine (Path.Combine (Path.Combine (prefix, "lib"), "mono"), "gac");
				if (Directory.Exists (gac_path) && !paths.Contains (gac))
					paths.Add (gac_path);
			}

			return paths;
		}

		static string GetCurrentMonoGac ()
		{
			return Path.Combine (
				Directory.GetParent (
					Path.GetDirectoryName (typeof(object).Module.FullyQualifiedName)).FullName,
				"gac");
		}

		List<string> gac_paths;

		AssemblyDefinition GetAssemblyInGac (AssemblyNameReference reference)
		{
			if (reference.PublicKeyToken == null || reference.PublicKeyToken.Length == 0)
				return null;

			if (gac_paths == null)
				gac_paths = GetGacPaths ();

			if (OnMono ())
				return GetAssemblyInMonoGac (reference);

			return GetAssemblyInNetGac (reference);
		}

		AssemblyDefinition GetAssemblyInMonoGac (AssemblyNameReference reference)
		{
			for (int i = 0; i < gac_paths.Count; i++) {
				var gac_path = gac_paths [i];
				var file = GetAssemblyFile (reference, string.Empty, gac_path);
				if (File.Exists (file))
					return GetAssembly (file);
			}

			return null;
		}

		AssemblyDefinition GetAssemblyInNetGac (AssemblyNameReference reference)
		{
			var gacs = new[] { "GAC_MSIL", "GAC_32", "GAC" };
			var prefixes = new[] { string.Empty, "v4.0_" };

			for (int i = 0; i < 2; i++) {
				for (int j = 0; j < gacs.Length; j++) {
					var gac = Path.Combine (gac_paths [i], gacs [j]);
					var file = GetAssemblyFile (reference, prefixes [i], gac);
					if (Directory.Exists (gac) && File.Exists (file))
						return GetAssembly (file);
				}
			}

			return null;
		}

		static string GetAssemblyFile (AssemblyNameReference reference, string prefix, string gac)
		{
			var gac_folder = new StringBuilder ()
				.Append (prefix)
				.Append (reference.Version)
				.Append ("__");

			for (int i = 0; i < reference.PublicKeyToken.Length; i++)
				gac_folder.Append (reference.PublicKeyToken [i].ToString ("x2"));

			return Path.Combine (
				Path.Combine (
					Path.Combine (gac, reference.Name), gac_folder.ToString ()),
				reference.Name + ".dll");
		}


	}

}
