using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;

namespace GithubPullTracker.Models
{
    public class PageMap
    {
        public PageMap(string patch)
        {
            if (string.IsNullOrWhiteSpace(patch))
            {
                return;
            }
            var lines = patch.Split('\n');

            int sourcePageIndex = 0;
            int targetPageIndex = 0;
            for (var line = 1; line <= lines.Length; line++)
            {
                try
                {
                    var text = lines[line - 1];
                    if (text.StartsWith("@@"))
                    {
                        var match = Regex.Match(text, @"^\@\@\s?(?:-(\d*),(\d*))?\s?(?:\+(\d*),(\d*))?\s?\@\@");
                        sourcePageIndex = match.Groups[1].Success ? int.Parse(match.Groups[1].Value) - 1 : -1;
                        targetPageIndex = match.Groups[3].Success ? int.Parse(match.Groups[3].Value) - 1 : -1;
                    }
                    else
                    {
                        var prefix = text[0];
                        if (prefix == '-')
                        {
                            SourceFile.Add(++sourcePageIndex, line);
                            SourceFileInverse.Add(line, sourcePageIndex);
                        }
                        else if (prefix == ' ')
                        {
                            TargetFile.Add(++targetPageIndex, line);
                            SourceFile.Add(++sourcePageIndex, -1);

                            TargetFileInverse.Add(line, targetPageIndex);
                            SourceFileInverse.Add(line, sourcePageIndex);
                            //++sourcePageStart;//only increment source but allow commenting on right
                        }
                        if (prefix == '+')
                        {
                            TargetFile.Add(++targetPageIndex, line);
                            TargetFileInverse.Add(line, targetPageIndex);
                        }
                    }
                }
                catch (Exception ex)
                {
                    throw;
                }
            }
        }

        public IDictionary<int, int> SourceFile = new Dictionary<int, int>();
        public IDictionary<int, int> TargetFile = new Dictionary<int, int>();
        private IDictionary<int, int> SourceFileInverse = new Dictionary<int, int>();
        private IDictionary<int, int> TargetFileInverse = new Dictionary<int, int>();

        public int? PatchToTargetLineNumber(int? patchnumber)
        {
            if (patchnumber == null)
            {
                return null;
            }
            if (TargetFileInverse.ContainsKey(patchnumber.Value))
            {
                return TargetFileInverse[patchnumber.Value];
            }
            return null;
        }

        public int? PatchToSourceLineNumber(int? patchnumber)
        {
            if (patchnumber == null)
            { return null; }

            if (SourceFileInverse.ContainsKey(patchnumber.Value))
            {
                return SourceFileInverse[patchnumber.Value];
            }
            return null;
        }
    }
}