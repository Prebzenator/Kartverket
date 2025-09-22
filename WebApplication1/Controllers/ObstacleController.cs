using Microsoft.AspNetCore.Mvc;
using WebApplication1.Models;

namespace WebApplication1.Controllers
{
    public class ObstacleController : Controller
    {
        //public IActionResult Index()
        //{
        //return View();
        //}

        [HttpGet]
        public ActionResult DataForm()
        {
            return View();
        }









        [HttpPost]
        public ActionResult DataForm(ObstacleData obstacledata)
        {
            if (!ModelState.IsValid)
            {
                return View(obstacledata);
            }
            return View("Overview", obstacledata);
        }


    }
}
