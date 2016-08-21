using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace GithubPullTracker.Models
{
    public class FileView
    {
        public FileView(string login, string owner, string repo, int no, string filesha)
        {
            this.Repo = repo;
            this.Owner = owner;
            this.Repo = repo;
            this.Number = no;
            this.FileSha = filesha;
            this.Login = login;
            CreatedDate = DateTime.UtcNow;
        }
        
        public FileView() { }

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Index("RepoFileViews", Order = 1)]
        [MaxLength(256)]
        public string Login { get; set; }

        [Index("RepoFileViews", Order = 2)]
        [MaxLength(256)]
        public string Repo { get; set; }

        [Index("RepoFileViews", Order = 3)]
        [MaxLength(256)]
        public string Owner { get; set; }

        [Index("RepoFileViews", Order = 4)]
        public int Number { get; set; }


        public string FileSha { get; set; }

        public DateTime CreatedDate { get; set; }
    }
}