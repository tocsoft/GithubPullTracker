using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Web;
using Newtonsoft.Json.Linq;
using Octokit;

namespace GithubPullTracker.Models
{
    public class PullRequestFileView : PullRequestView
    {
        PullRequestViewItem rootItem;

        public PullRequestFileView(PullRequest pr, IEnumerable<PullRequestFile> files):base(pr)
        {
            rootItem = new PullRequestViewItem(files);
        }

        public PullRequestFileView(PullRequest pr, IEnumerable<PullRequestFile> files, string selectedPath, string targetText, bool isBinary) : this(pr, files)
        {
            this.TargetText = targetText;
            this.IsBinary = isBinary;
            CurrentFile = rootItem.GetItem(selectedPath);

        }
        public PullRequestViewItem CurrentFile { get; set; }

        public IEnumerable<PullRequestViewItem> Items { get { return rootItem?.Children; } }
        public IEnumerable<PullRequestViewItem> FileItems { get { return rootItem?.Decendents.Where(x=>x.IsFile); } }

        public string TreeDataJson
        {
            get
            {
                return Newtonsoft.Json.JsonConvert.SerializeObject(Items.Select(x => x.TreeSettingsObject).ToArray());                
            }
        }
        
        public string TargetText { get; private set; }
        public bool IsBinary { get; private set; }
    }
}