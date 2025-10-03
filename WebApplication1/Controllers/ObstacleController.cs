using Microsoft.AspNetCore.Mvc;
using WebApplication1.Models;

/// Handles routes for obstacle data input and overview display (Obstacle registration)
/// - GET /Obstacle/DataForm: Displays the form for entering obstacle data
/// - POST /Obstacle/DataForm: Handles form submission and shows overview if valid
/// - GET /Obstacle/Overview: Displays the overview of submitted obstacle data

namespace WebApplication1.Controllers
{
    public class ObstacleController : Controller
    {
        //public IActionResult Index()
        //{
        //return View();
        //}


        // This action displays the form for entering obstacle data (GET)
        [HttpGet]
        public ActionResult DataForm()
        {
            return View();
        }



        // This action handles the form submission for the obstacle data (POST)
        // If the model is valid, it shows the overview page with the submitted data
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
