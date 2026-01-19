using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Obfuscar
{
    internal class Filter : IEnumerable<string>
    {
        private static readonly char[] directorySeparators = new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar };
        private readonly IList<string> inclusions, exclusions;
        private readonly string path;

        public Filter(string path, IList<string> inclusions, IList<string> exclusions)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentException("message", nameof(path));
            }

            this.path = path;
            this.inclusions = inclusions ?? throw new ArgumentNullException(nameof(inclusions));
            this.exclusions = exclusions ?? throw new ArgumentNullException(nameof(exclusions));
        }

        public IEnumerator<string> GetEnumerator() => GetFiles().GetEnumerator();

        private IEnumerable<string> GetFiles()
        {
            var excluded = new HashSet<string>(exclusions.SelectMany(GetFiles), StringComparer.Ordinal);
            return inclusions.SelectMany(GetFiles).Where(file => !excluded.Contains(file));
        }

        private IEnumerable<string> GetFiles(string pattern)
        {
            var lastSeparator = pattern.LastIndexOfAny(directorySeparators);
            var searchPath = lastSeparator != -1 ?
                Path.GetFullPath(Path.Combine(path, pattern.Substring(0, lastSeparator))) :
                path;
            var filePattern = lastSeparator != -1 ?
                pattern.Substring(lastSeparator + 1) :
                pattern;
            return Directory.EnumerateFiles(searchPath, filePattern);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }
    }
}
