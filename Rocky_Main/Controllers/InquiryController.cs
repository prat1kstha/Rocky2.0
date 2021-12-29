using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Rocky_DataAccess;
using Rocky_Utility;
using Rocky_Models;
using Rocky_Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Rocky_DataAccess.Repository.IRepository;

namespace Rocky.Controllers
{
    [Authorize(Constants.AdminRole)]
    public class InquiryController : Controller
    {
        private readonly IInquiryDetailRepository _inquiryDetailRepo;
        private readonly IInquiryHeaderRepository _inquiryHeaderRepo;
        [BindProperty]
        public InquiryVM InquiryVM { get; set; }

        public InquiryController(IInquiryDetailRepository inquiryDetailRepo, IInquiryHeaderRepository inquiryHeaderRepo)
        {
            _inquiryDetailRepo = inquiryDetailRepo;
            _inquiryHeaderRepo = inquiryHeaderRepo;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Details(int id)
        {
            InquiryVM = new InquiryVM()
            {
                InquiryHeader = _inquiryHeaderRepo.FirstOrDefault(u => u.Id == id),
                InquiryDetail = _inquiryDetailRepo.GetAll(u=>u.InquiryHeaderId == id, includeProperties:"Product")
            };

            return View(InquiryVM);
        }

        [HttpPost, AutoValidateAntiforgeryToken]
        public IActionResult Details()
        {
            List<ShoppingCart> shoppingCartList = new List<ShoppingCart>();

            InquiryVM.InquiryDetail = _inquiryDetailRepo.GetAll(u => u.InquiryHeaderId == InquiryVM.InquiryHeader.Id);

            foreach (var detail in InquiryVM.InquiryDetail)
            {
                ShoppingCart shoppingCart = new ShoppingCart()
                {
                    ProductId = detail.ProductId
                };

                shoppingCartList.Add(shoppingCart);
            }

            HttpContext.Session.Clear();
            HttpContext.Session.Set(Constants.SessionCart, shoppingCartList);
            HttpContext.Session.Set(Constants.SessionInquiryId, InquiryVM.InquiryHeader.Id);
            return RedirectToAction("Index","Cart");
        }

        [HttpPost]
        public IActionResult Delete()
        {
            InquiryHeader inquiryHeader = _inquiryHeaderRepo.FirstOrDefault(u => u.Id == InquiryVM.InquiryHeader.Id);
            IEnumerable<InquiryDetail> inquiryDetails = _inquiryDetailRepo.GetAll(u => u.InquiryHeaderId == InquiryVM.InquiryHeader.Id);

            _inquiryDetailRepo.RemoveRange(inquiryDetails);
            _inquiryHeaderRepo.Remove(inquiryHeader);
            _inquiryHeaderRepo.Save();

            return RedirectToAction(nameof(Index));

        }

        #region API Calls
        [HttpGet]
        public IActionResult GetInquiryList()
        {
            return Json(new { data = _inquiryHeaderRepo.GetAll() });
        }

        #endregion
    }
}
