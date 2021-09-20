using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Smartstore.Web.Controllers;

namespace Smartstore.Forums.Controllers
{
    // TODO: (mg) (core) add forum activity logs. They have never been used in frontend!?
    public class ForumController : PublicController
    {
        public ForumController()
        {
        }

        public Task<IActionResult> ForumGroup(int id)
        {
            throw new NotImplementedException();
        }

        public Task<IActionResult> ForumTopic(int id)
        {
            throw new NotImplementedException();
        }

        public Task<IActionResult> Forum(int id)
        {
            throw new NotImplementedException();
        }
    }
}
