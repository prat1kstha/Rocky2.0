using Microsoft.AspNetCore.Mvc;
using Rocky_DataAccess;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Rocky_Models;
using Rocky_Utility;
using Microsoft.AspNetCore.Mvc.Rendering;
using Rocky_Models.ViewModels;
using Microsoft.AspNetCore.Hosting;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace Rocky.Controllers
{
    [Authorize(Roles = Constants.AdminRole)]
    public class ProductController : Controller
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public ProductController(ApplicationDbContext dbContext, IWebHostEnvironment webHostEnvironment)
        {
            _dbContext = dbContext;
            _webHostEnvironment = webHostEnvironment;
        }

        public IActionResult Index()
        {
            IEnumerable<Product> objList = _dbContext.Product.Include(u=>u.Category).Include(u=>u.ApplicationType);
            //foreach (var obj in objList)
            //{
            //    obj.Category = _dbContext.Category.FirstOrDefault(x => x.Id == obj.CategoryId);
            //    obj.ApplicationType = _dbContext.ApplicationType.FirstOrDefault(x => x.Id == obj.ApplicationTypeId);
            //};

            return View(objList);
        }


        //GET - UpdateInsert
        public IActionResult UpdateInsert(int? id)
        {
            #region CreatingCategoryListWithViewBag
            //Product product = new Product();
            //IEnumerable<SelectListItem> CategorySelectList = _dbContext.Category.Select(i => new SelectListItem
            //{
            //    Text = i.CategoryName,
            //    Value = i.Id.ToString()
            //});

            ////ViewBag.CategorySelectList = CategorySelectList;
            #endregion

            #region CreatingCategoryListWithViewModel
            ProductVM productVM = new ProductVM()
            {
                Product = new Product(),
                CategorySelectList = _dbContext.Category.Select(i => new SelectListItem
                {
                    Text = i.CategoryName,
                    Value = i.Id.ToString()
                }),
                ApplicationTypeSelectList = _dbContext.ApplicationType.Select(i=> new SelectListItem { 
                    Text = i.ApplicationName,
                    Value = i.Id.ToString()
                })
            };
            #endregion

            if (id == null)
            {
                return View(productVM);
            }
            else
            {
                productVM.Product = _dbContext.Product.Find(id);
                if (productVM == null)
                {
                    return NotFound();
                }
                return View(productVM);
            }
        }

        //POST - UpdateInsert
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateInsert(ProductVM productVM)
        {
            if (ModelState.IsValid)
            {
                var files = HttpContext.Request.Form.Files;
                string webRootPath = _webHostEnvironment.WebRootPath;

                if (productVM.Product.Id == 0)
                {
                    //Create
                    string upload = webRootPath + Constants.ImagePath;
                    string fileName = Guid.NewGuid().ToString();
                    string extension = Path.GetExtension(files[0].FileName);

                    using (var fileStream = new FileStream(Path.Combine(upload, fileName + extension), FileMode.Create))
                    {
                        files[0].CopyTo(fileStream);
                    }

                    productVM.Product.Image = fileName + extension;

                    _dbContext.Product.Add(productVM.Product);
                }
                else
                {
                    //Update
                    var obj = _dbContext.Product.AsNoTracking().FirstOrDefault(u => u.Id == productVM.Product.Id);

                    if (files.Count > 0)
                    {
                        string upload = webRootPath + Constants.ImagePath;
                        string fileName = Guid.NewGuid().ToString();
                        string extension = Path.GetExtension(files[0].FileName);

                        var oldFile = Path.Combine(upload, obj.Image);
                        if (System.IO.File.Exists(oldFile))
                        {
                            System.IO.File.Delete(oldFile);
                        }

                        using (var fileStream = new FileStream(Path.Combine(upload, fileName + extension), FileMode.Create))
                        {
                            files[0].CopyTo(fileStream);
                        }

                        productVM.Product.Image = fileName + extension;
                    }
                    else
                    {
                        productVM.Product.Image = obj.Image;
                    }

                    _dbContext.Product.Update(productVM.Product);
                }
                _dbContext.SaveChanges();
                return RedirectToAction("Index");
            }

            productVM.CategorySelectList = _dbContext.Category.Select(i => new SelectListItem
            {
                Text = i.CategoryName,
                Value = i.Id.ToString()
            });

            productVM.ApplicationTypeSelectList = _dbContext.ApplicationType.Select(i => new SelectListItem
            {
                Text = i.ApplicationName,
                Value = i.Id.ToString()
            });
            return View(productVM);
        }



        //GET - Delete
        public IActionResult Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            Product product = _dbContext.Product.Include(u => u.Category).Include(u => u.ApplicationType).FirstOrDefault(u => u.Id == id);
            if (product == null)
            {
                return NotFound();
            }
            return View(product);
        }

        //POST - Delete
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeletePost(int? id)
        {
            var obj = _dbContext.Product.Find(id);
            if (obj == null)
            {
                return NotFound();
            }

            string upload = _webHostEnvironment.WebRootPath + Constants.ImagePath;

            var oldFile = Path.Combine(upload, obj.Image);
            if (System.IO.File.Exists(oldFile))
            {
                System.IO.File.Delete(oldFile);
            }
            _dbContext.Product.Remove(obj);
            _dbContext.SaveChanges();
            return RedirectToAction("Index");
        }
    }
}
