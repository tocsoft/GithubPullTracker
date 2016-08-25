using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Web;
using GithubClient.Models;
using Newtonsoft.Json.Linq;

namespace GithubPullTracker.Models
{
    public class PullRequestFileView 
    {
        PullRequestViewItem rootItem;
        
        public PullRequestFileView(PullRequestView header, IEnumerable<CommitFile> files, string selectedPath, string targetText, bool isBinary, IEnumerable<CommitComment> comments, IEnumerable<string> visistedFileShas)
        {
            rootItem = new PullRequestViewItem(files);
            this.TargetText = targetText;
            this.IsBinary = isBinary;
            CurrentFile = rootItem.GetItem(selectedPath);
            this.VisitedFiles = visistedFileShas;
            this.CommentList = comments;
            this.Details = header;
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
        public IEnumerable<CommitComment> CommentList { get; private set; }
        public IEnumerable<string> VisitedFiles { get; private set; }
        public PullRequestView Details { get; private set; }
    }
}