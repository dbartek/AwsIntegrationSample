using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using AmazonWebApp.Amazon;
using AmazonWebApp.Models;
using Microsoft.Extensions.Configuration;

namespace AmazonWebApp.Controllers
{
    public class HomeController : Controller
    {
        private AmazonClient _amazonClient;

        public HomeController(AmazonClient amazonClient)
        {
            _amazonClient = amazonClient;         
        }

        public IActionResult Index()
        {
            var successUrl = Request.GetEncodedUrl();
            var model = _amazonClient.BuildUploadModel(successUrl);

            try
            {
                ViewBag.Pictures = _amazonClient.ListPictures();
            }
            catch 
            {
                ViewBag.Message = "Unable to load picture list...";
                ViewBag.Pictures = Enumerable.Empty<PictureViewModel>();
            }
            

            return View(model);
        }

        public IActionResult Image(string fileName)
        {
            try
            {
                var img = _amazonClient.GetPicture(fileName);
                ViewBag.Image = $"data:image/png;base64,{Convert.ToBase64String(img)}";
            }
            catch
            {
                ViewBag.Image = null;
            }
            
            return View();
        }

        [HttpPost]
        public IActionResult Transform(PicturesTransformationModel model)
        {
            if (model != null)
            {
                _amazonClient.RequestPictureTransformations(model);
            }

            return Json("OK");
        }

        public IActionResult Error()
        {
            return View();
        }
    }
}
