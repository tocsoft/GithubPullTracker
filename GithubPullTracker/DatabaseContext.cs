using GithubPullTracker.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;

namespace GithubPullTracker
{
    public class DatabaseContext : DbContext
    {
        public DatabaseContext() : base(SettingsManager.Settings.GetSetting("DatabaseConnectionString"))
        {
            this.Views = Set<FileView>();
        }

        public IDbSet<FileView> Views { get; }
    }
}