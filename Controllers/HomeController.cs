using System;
using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using EcoRecycle.DAL;
using EcoRecycle.Models;

namespace EcoRecycle.Controllers
{
    public class HomeController : Controller
    {
        private readonly ContentDAL _contentDal;

        public HomeController(ContentDAL contentDal)
        {
            _contentDal = contentDal;
        }

        public IActionResult Index()
        {
            var news = _contentDal.GetNewsList();
            var tips = _contentDal.GetEcoTips();

            ViewBag.News = news.Count > 3 ? news.GetRange(0, 3) : news;
            ViewBag.Tips = tips.Count > 4 ? tips.GetRange(0, 4) : tips;

            return View();
        }

        public IActionResult NewsDetails(int id)
        {
            var item = _contentDal.GetNewsById(id);
            if (item == null) return NotFound();
            return View(item);
        }

        public IActionResult EcoTips()
        {
            var tips = _contentDal.GetEcoTips();
            return View(tips);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
