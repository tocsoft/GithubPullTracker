using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace GithubPullTracker.Controllers
{
    public class RepositoryController : ControllerBase
    {
        // GET: Repository
        public ActionResult Index()
        {
            return View();
        }

        //configure repo
    }
}