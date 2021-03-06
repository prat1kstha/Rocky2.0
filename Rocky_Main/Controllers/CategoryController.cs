using Microsoft.AspNetCore.Mvc;
using Rocky_DataAccess;
using System;
using System.Collections.Generic;
using System.Linq;
using Rocky_Utility;
using System.Threading.Tasks;
using Rocky_Models;
using Microsoft.AspNetCore.Authorization;
using Rocky_DataAccess.Repository.IRepository;

namespace Rocky.Controllers
{
    [Authorize(Roles = Constants.AdminRole)]
    public class CategoryController : Controller
    {
        private readonly ICategoryRepository _catRepo;

        public CategoryController(ICategoryRepository catRepo)
        {
            _catRepo = catRepo;
        }

        public IActionResult Index()
        {
            IEnumerable<Category> objList = _catRepo.GetAll();
            return View(objList);
        }

        //GET - Create
        public IActionResult Create()
        {
            return View();
        }

        //POST - Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Category obj)
        {
            if (ModelState.IsValid)
            {
                _catRepo.Add(obj);
                _catRepo.Save();
                TempData[Constants.Success] = "Category created successfully";
                return RedirectToAction("Index");
            }
            TempData[Constants.Error] = "Error while creating category";
            return View(obj);
        }

        //GET - Edit
        public IActionResult Edit(int id)
        {
            if (id == 0)
            {
                return NotFound();
            }
            var obj = _catRepo.Find(id);
            if (obj == null)
            {
                return NotFound();
            }

            return View(obj);
        }

        //POST - Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(Category obj)
        {
            var x = obj;
            if (ModelState.IsValid)
            {
                _catRepo.Update(obj);
                _catRepo.Save();
                TempData[Constants.Success] = "Data updated successfully";
                return RedirectToAction("Index");
            }
            return View(obj);
        }


        //GET - Delete
        public IActionResult Delete(int? id)
        {
            if (id == 0)
            {
                return NotFound();
            }
            var obj = _catRepo.Find(id.GetValueOrDefault());
            if (obj == null)
            {
                return NotFound();
            }

            return View(obj);
        }

        //POST - Delete
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeletePost(int id)
        {
            var obj = _catRepo.Find(id);
            if (obj == null)
            {
                return NotFound();
            }
            else
            {
                _catRepo.Remove(obj);
                _catRepo.Save();
                TempData[Constants.Error] = "Data removed";
                return RedirectToAction("Index");
            }
        }
    }
}
