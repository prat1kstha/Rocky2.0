using Braintree;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Rocky_DataAccess;
using Rocky_DataAccess.Repository.IRepository;
using Rocky_Models;
using Rocky_Models.ViewModels;
using Rocky_Utility;
using Rocky_Utility.BrainTree;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Rocky.Controllers
{
    [Authorize]
    public class CartController : Controller
    {
        private readonly IProductRepository _prodRepo;
        private readonly IApplicationUserRepository _appUserRepo;
        private readonly IInquiryHeaderRepository _inquiryHeaderRepo;
        private readonly IInquiryDetailRepository _inquiryDetailRepo;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IEmailSender _emailSender;
        private readonly IOrderDetailRepository _orderDetailRepo;
        private readonly IOrderHeaderRepository _orderHeaderRepo;
        private readonly IBrainTreeGate _brain;

        [BindProperty]
        public ProductUserVM ProductUserVM { get; set; }
        public CartController(IProductRepository prodRepo, IApplicationUserRepository appUserRepo, 
            IInquiryDetailRepository inquiryDetailRepo, IInquiryHeaderRepository inquiryHeaderRepo,
            IOrderDetailRepository orderDetailRepo, IOrderHeaderRepository orderHeaderRepo,
            IWebHostEnvironment webHostEnvironment, IEmailSender emailSender,
            IBrainTreeGate brain)
        {
            _prodRepo = prodRepo;
            _webHostEnvironment = webHostEnvironment;
            _emailSender = emailSender;
            _appUserRepo = appUserRepo;
            _inquiryDetailRepo = inquiryDetailRepo;
            _inquiryHeaderRepo = inquiryHeaderRepo;
            _orderDetailRepo = orderDetailRepo;
            _orderHeaderRepo = orderHeaderRepo;
            _brain = brain;
        }
        public IActionResult Index()
        {
            List<ShoppingCart> shoppingCartList = new List<ShoppingCart>();
            if (HttpContext.Session.Get<IEnumerable<ShoppingCart>>(Constants.SessionCart) != null
                && HttpContext.Session.Get<IEnumerable<ShoppingCart>>(Constants.SessionCart).Count() > 0)
            {
                shoppingCartList = HttpContext.Session.Get<List<ShoppingCart>>(Constants.SessionCart);
            }

            List<int> productInCart = shoppingCartList.Select(i => i.ProductId).ToList();
            IEnumerable<Product> productListTemp = _prodRepo.GetAll(u => productInCart.Contains(u.Id));
            IList<Product> productList = new List<Product>();

            foreach (var cartObj in shoppingCartList)
            {
                Product prodTemp = productListTemp.FirstOrDefault(u => u.Id == cartObj.ProductId);
                prodTemp.TempSqFt = cartObj.SqFt;
                productList.Add(prodTemp);
            }

            return View(productList);
        }

        [HttpPost, ValidateAntiForgeryToken, ActionName("Index")]
        public IActionResult IndexPost(IEnumerable<Product> ProdList)
        {
            List<ShoppingCart> shoppingCartList = new List<ShoppingCart>();
            foreach (Product product in ProdList)
            {
                shoppingCartList.Add(new ShoppingCart { ProductId = product.Id, SqFt = product.TempSqFt });
            }

            HttpContext.Session.Set(Constants.SessionCart, shoppingCartList);
            return RedirectToAction(nameof(Summary));
        }

        public IActionResult Summary()
        {
            ApplicationUser applicationUser;

            if (User.IsInRole(Constants.AdminRole))
            {
                if (HttpContext.Session.Get<int>(Constants.SessionInquiryId) != 0)
                {
                    //cart has been loaded using an Inquiry
                    InquiryHeader inquiryHeader = _inquiryHeaderRepo.FirstOrDefault(u => u.Id == HttpContext.Session.Get<int>(Constants.SessionInquiryId));
                    applicationUser = new ApplicationUser() 
                    { 
                        Email = inquiryHeader.Email, 
                        FullName = inquiryHeader.FullName, 
                        PhoneNumber = inquiryHeader.PhoneNumber 
                    };
                }
                else
                {
                    applicationUser = new ApplicationUser();
                }

                var gateway = _brain.GetGateway();
                var clientToken = gateway.ClientToken.Generate();
                ViewBag.ClientToken = clientToken;

            }
            else
            {
                var claimsIdentity = (ClaimsIdentity)User.Identity;
                var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);
                applicationUser = _appUserRepo.FirstOrDefault(u => u.Id == claim.Value);
            }
            
            //var userId = User.FindFirstValue(ClaimTypes.Name);

            List<ShoppingCart> shoppingCartList = new List<ShoppingCart>();
            if (HttpContext.Session.Get<IEnumerable<ShoppingCart>>(Constants.SessionCart) != null
                && HttpContext.Session.Get<IEnumerable<ShoppingCart>>(Constants.SessionCart).Count() > 0)
            {
                shoppingCartList = HttpContext.Session.Get<List<ShoppingCart>>(Constants.SessionCart);
            }

            List<int> productInCart = shoppingCartList.Select(i => i.ProductId).ToList();
            IEnumerable<Product> productList = _prodRepo.GetAll(u => productInCart.Contains(u.Id));

            ProductUserVM = new ProductUserVM()
            {
                ApplicationUser = applicationUser,
            };

            foreach (var cartObj in shoppingCartList)
            {
                Product prodTemp = _prodRepo.FirstOrDefault(u => u.Id == cartObj.ProductId);
                prodTemp.TempSqFt = cartObj.SqFt;
                ProductUserVM.ProductList.Add(prodTemp);
            }
            return View(ProductUserVM);
        }

        [HttpPost, ValidateAntiForgeryToken, ActionName("Summary")]
        public async Task<IActionResult> SummaryPost(IFormCollection collection, ProductUserVM productUserVM)
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

            if (User.IsInRole(Constants.AdminRole))
            {
                //need to create an order
                //var orderTotal = 0.0;
                //foreach (Product product in ProductUserVM.ProductList)
                //{
                //    orderTotal += product.Price * product.TempSqFt;
                //}
                OrderHeader orderHeader = new OrderHeader()
                {
                    CreatedByUserId = claim.Value,
                    FinalOrderTotal = ProductUserVM.ProductList.Sum(x => x.TempSqFt * x.Price),
                    City = productUserVM.ApplicationUser.City,
                    StreetAddress = productUserVM.ApplicationUser.StreetAddress,
                    State = productUserVM.ApplicationUser.State,
                    PostalCode = productUserVM.ApplicationUser.PostalCode,
                    FullName = productUserVM.ApplicationUser.FullName,
                    Email = productUserVM.ApplicationUser.Email,
                    PhoneNumber = productUserVM.ApplicationUser.PhoneNumber,
                    OrderDate = DateTime.Now,
                    OrderStatus = Constants.StatusPending
                };

                _orderHeaderRepo.Add(orderHeader);
                _orderHeaderRepo.Save();

                foreach (var prod in ProductUserVM.ProductList)
                {
                    OrderDetail orderDetail = new OrderDetail()
                    {
                        OrderHeaderId = orderHeader.Id,
                        PricePerSqFt = prod.Price,
                        Sqft = prod.TempSqFt,
                        ProductId = prod.Id
                    };
                    _orderDetailRepo.Add(orderDetail);
                }
                _orderDetailRepo.Save();

                string nonceFromTheClient = collection["payment_method_nonce"];
                var request = new TransactionRequest
                {
                    Amount = Convert.ToDecimal(orderHeader.FinalOrderTotal),
                    PaymentMethodNonce = nonceFromTheClient,
                    OrderId = orderHeader.Id.ToString(),
                    Options = new TransactionOptionsRequest
                    {
                        SubmitForSettlement = true
                    }
                };

                var gateway = _brain.GetGateway();
                Result<Transaction> result = gateway.Transaction.Sale(request);

                if (result.Target.ProcessorResponseText == "Approved")
                {
                    orderHeader.TransactionId = result.Target.Id;
                    orderHeader.OrderStatus = Constants.StatusApproved;
                }
                else
                {
                    orderHeader.OrderStatus = Constants.StatusCancelled;
                }
                _orderHeaderRepo.Save();

                return RedirectToAction(nameof(InquiryConfirmation), new { id = orderHeader.Id });
            }
            else
            {
                //need to create an enquiry
                var PathToTemplate = _webHostEnvironment.WebRootPath + Path.DirectorySeparatorChar.ToString()
                + "Templates" + Path.DirectorySeparatorChar.ToString()
                + "Inquiry.html";

                var subject = "New Inquiry";
                string HtmlBody = "";

                using (StreamReader sr = System.IO.File.OpenText(PathToTemplate))
                {
                    HtmlBody = sr.ReadToEnd();
                }

                StringBuilder productListSB = new StringBuilder();
                foreach (var product in ProductUserVM.ProductList)
                {
                    productListSB.Append($" - Name: {product.ProductName} <span style='font-size:14px'> (ID: {product.Id}) </span> <br />");
                }

                string messageBody = string.Format(HtmlBody,
                    productUserVM.ApplicationUser.FullName,
                    productUserVM.ApplicationUser.Email,
                    productUserVM.ApplicationUser.PhoneNumber,
                    productListSB.ToString());

                await _emailSender.SendEmailAsync(Constants.EmailAdmin, subject, messageBody);

                InquiryHeader inquiryHeader = new InquiryHeader()
                {
                    ApplicationUserId = claim.Value,
                    FullName = productUserVM.ApplicationUser.FullName,
                    Email = productUserVM.ApplicationUser.Email,
                    PhoneNumber = productUserVM.ApplicationUser.PhoneNumber,
                    InquiryDate = DateTime.Now
                };

                _inquiryHeaderRepo.Add(inquiryHeader);
                _inquiryHeaderRepo.Save();

                foreach (var prod in ProductUserVM.ProductList)
                {
                    InquiryDetail inquiryDetail = new InquiryDetail()
                    {
                        InquiryHeaderId = inquiryHeader.Id,
                        ProductId = prod.Id
                    };

                    _inquiryDetailRepo.Add(inquiryDetail);
                }
                _inquiryDetailRepo.Save();
                TempData[Constants.Success] = "Cart updated successfully";

            }

            return RedirectToAction(nameof(InquiryConfirmation));
        }

        public IActionResult InquiryConfirmation(int id = 0)
        {
            OrderHeader orderHeader = _orderHeaderRepo.FirstOrDefault(u => u.Id == id);
            HttpContext.Session.Clear();
            return View(orderHeader);
        }

        public IActionResult Remove(int id)
        {
            List<ShoppingCart> shoppingCartList = new List<ShoppingCart>();
            if (HttpContext.Session.Get<IEnumerable<ShoppingCart>>(Constants.SessionCart) != null
                && HttpContext.Session.Get<IEnumerable<ShoppingCart>>(Constants.SessionCart).Count() > 0)
            {
                shoppingCartList = HttpContext.Session.Get<List<ShoppingCart>>(Constants.SessionCart);
            }

            shoppingCartList.Remove(shoppingCartList.FirstOrDefault(u => u.ProductId == id));

            HttpContext.Session.Set(Constants.SessionCart, shoppingCartList);
            TempData[Constants.Error] = "Item removed";

            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult UpdateCart(IEnumerable<Product> ProdList)
        {
            List<ShoppingCart> shoppingCartList = new List<ShoppingCart>();
            foreach (Product product in ProdList)
            {
                shoppingCartList.Add(new ShoppingCart { ProductId = product.Id, SqFt = product.TempSqFt });
            }

            HttpContext.Session.Set(Constants.SessionCart, shoppingCartList);
            return RedirectToAction(nameof(Index));
        }
    }
}
