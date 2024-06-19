using System;
using System.IO;
using System.CodeDom.Compiler;
using System.Text;
using System.Linq;
using System.Collections.Generic;

namespace ObfuscarTestNet
{
    static class TestHelper
    {
        public static string InputPath = Path.Combine("..", "..", "..", "Input");

        private static int count;

        public static string OutputPath
        {
            get { return Path.Combine("..", "..", "Output", count++.ToString()); }
        }

        public static void CleanInput()
        {
            // clean out inputPath
            try
            {
                //foreach (string file in Directory.GetFiles(InputPath, "*.dll"))
                //File.Delete(file);
            }
            catch
            {
            }
        }

        public static string BuildAssembly(string name, params string[] references)
        {
            var builder = new SourceBuilder();
            return builder.Build(Path.Combine(InputPath, name + ".cs"), references);
        }

        public static void BuildAssemblies(params string[] names)
        {
            var references = new List<string>();
            foreach (var name in names)
            {
                references.Add(BuildAssembly(name, references.ToArray()));
            }
        }

        public static Obfuscar.Obfuscator Obfuscate(string xml, bool hideStrings = false)
        {
            Obfuscar.Obfuscator obfuscator = Obfuscar.Obfuscator.CreateFromXml(xml);

            if (hideStrings)
                obfuscator.HideStrings();

            var typeNamesInXaml = obfuscator.CollectTypesFromXaml();
            var allPropertyTypesRelatedToNpc = obfuscator.CollectTypesRelatedToNpc();
            var allTypeNamesRelatedToNpcOrInXaml = typeNamesInXaml
                .Union(allPropertyTypesRelatedToNpc)
                .ToHashSet();

            obfuscator.RenameFields(typeNamesInXaml);
            obfuscator.RenameParams();
            obfuscator.RenameProperties(allTypeNamesRelatedToNpcOrInXaml);
            obfuscator.RenameEvents();
            obfuscator.RenameMethods();
            obfuscator.RenameTypes(typeNamesInXaml);
            obfuscator.PostProcessing();
            obfuscator.SaveAssemblies(true);

            return obfuscator;
        }

        public static Obfuscar.Obfuscator BuildAndObfuscate(string name, string xml, bool hideStrings = false)
        {
            CleanInput();
            BuildAssembly(name);
            return Obfuscate(xml, hideStrings);
        }

        public static Obfuscar.Obfuscator BuildAndObfuscate(string[] names, string xml)
        {
            CleanInput();
            BuildAssemblies(names);
            return Obfuscate(xml);
        }

        private static string GetAssemblyPath(string name)
        {
            return Path.Combine(InputPath, name + ".dll");
        }
    }
}
