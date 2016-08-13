using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Web;
using Newtonsoft.Json.Linq;
using Octokit;

namespace GithubPullTracker.Models
{
    public class PullFileList
    {
        private PullRequest pr;

        public PullFileList(PullRequest pr, IEnumerable<PullRequestFile> files)
        {
            this.pr = pr;
            var rootItem = new PullRequestViewItem(files);
            Items = rootItem.Children;
        }

        public string RepositoryOwner { get; set; }

        public string RepositoryName { get; set; }

        public int Number { get; set; }

        public IEnumerable<PullRequestViewItem> Items { get; set; }

        public JObject TreeData
        {
            get {

                var obj = JObject.FromObject(new {
                    headSha = pr.Head.Sha,
                    baseSha = pr.Base.Sha,
                    data = new object[0]
                });
                foreach (var i in Items) {
                    (obj["data"] as JArray).Add(i.TreeSettingsObject);
                }
                return obj;
            }
        }

        [DebuggerDisplay("{Path}")]
        public class PullRequestViewItem
        {


            bool _selected = false;
            private readonly PullRequestFile fileItem;

            private static IEnumerable<PullRequestViewItem> Convert(PullRequestViewItem parent, IEnumerable<PullRequestFile> children)
            {
                var path = parent?.Path ?? "";

                var groupsByFile = children
                    .Where(c => c.FileName != path)
                    .Select(x => new { x, x.FileName }).ToList();

                var inFolder = groupsByFile
                    .Select(x => new { x.x, x.FileName, parts = x.FileName.Substring(path.Length).TrimStart('/').Split('/') }).ToList();

                var grouped = inFolder.GroupBy(x => x.parts[0]).ToList();

                var converted = grouped.Select(x => new PullRequestViewItem(x.Key, parent, x.Select(g => g.x))).ToList();

                return converted;
            }

            public PullRequestViewItem(IEnumerable<PullRequestFile> children)
            {
                this.Name = null;
                this.Children = Convert(this, children);
            }

            public PullRequestViewItem(string name, PullRequestViewItem parent, IEnumerable<PullRequestFile> children)
            {
                this.Name = name;
                this.Parent = parent;
                this.Children = Convert(this, children);

                fileItem = children.SingleOrDefault(x => x.FileName == Path);                
            }

            public bool IsFile
            {
                get { return fileItem != null; }
            }

            public bool Selected
            {
                get
                {
                    return _selected || Children.Any(x => x.Selected);
                }
                set
                {
                    _selected = value;
                }
            }

            public string Name { get; private set; }
            public PullRequestViewItem Parent { get; private set; }
            public IEnumerable<PullRequestViewItem> Children { get; private set; }

            public string Path
            {
                get
                {
                    if (Parent != null && Parent.Path != null)
                    {
                        return Parent.Path + "/" + Name;
                    }
                    else
                    {
                        return Name;
                    }
                }
            }
            public JObject TreeSettingsObject
            {
                get
                {
                    if (IsFile)
                    {
                        var obj = JObject.FromObject(
                        new
                        {
                            text = Name,
                            //type = IsFile ? "file" : "folder",
                            //we can infer the type based on if it has children or not as there  are no rway of mapping folders in git anyway
                            path = Path,
                            sha = fileItem.Sha,
                        });
                        return obj;
                    }
                    else
                    {
                        var obj = JObject.FromObject(
                        new
                        {
                            text = Name,
                        });
                        
                            JArray childrenList = new JArray();

                            foreach (var c in Children.Select(x => x.TreeSettingsObject))
                            {
                                childrenList.Add(c);
                            }
                            obj.Add("children", childrenList);

                        return obj;
                    }

                }
            }
            
            public PullRequestViewItem GetItem(string path)
            {
                if (path == Path)
                {
                    return this;
                }

                return Children.Select(x => x.GetItem(path)).Where(x=>x != null).FirstOrDefault();
            }
        }
    }
}