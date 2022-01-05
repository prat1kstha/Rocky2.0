using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Rocky_DataAccess.Repository.IRepository;
using Rocky_Models.ViewModels;
using Rocky_Utility;
using Rocky_Utility.BrainTree;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Rocky.Controllers
{
    public class OrderController : Controller
    {
        private readonly IOrderHeaderRepository _orderHeaderRepo;
        private readonly IOrderDetailRepository _orderDetailRepo;
        private readonly IBrainTreeGate _brain;
        [BindProperty]
        public OrderVM OrderVM { get; set; }

        public OrderController(IOrderHeaderRepository orderHeaderRepo, IOrderDetailRepository orderDetailRepo, IBrainTreeGate brain)
        {
            _orderHeaderRepo = orderHeaderRepo;
            _orderDetailRepo = orderDetailRepo;
            _brain = brain;
        }

        public IActionResult Index(string searchName = null, string searchEmail = null, string searchPhone = null, string Status = null)
        {
            OrderListVM orderListVM = new OrderListVM()
            {
                OrderHList = _orderHeaderRepo.GetAll(),
                StatusList = Constants.listStatus.ToList().Select(i => new SelectListItem
                {
                    Text = i,
                    Value = i
                })
            };

            if (!string.IsNullOrEmpty(searchName))
            {
                orderListVM.OrderHList = orderListVM.OrderHList.Where(u => u.FullName.ToLower().Contains(searchName.ToLower()));
            }
            if (!string.IsNullOrEmpty(searchEmail))
            {
                orderListVM.OrderHList = orderListVM.OrderHList.Where(u => u.Email.ToLower().Contains(searchEmail.ToLower()));
            }
            if (!string.IsNullOrEmpty(searchPhone))
            {
                orderListVM.OrderHList = orderListVM.OrderHList.Where(u => u.PhoneNumber.Contains(searchPhone));
            }
            if (!string.IsNullOrEmpty(Status) && Status!="--Order Status--")
            {
                orderListVM.OrderHList = orderListVM.OrderHList.Where(u => u.OrderStatus.ToLower().Contains(Status.ToLower()));
            }

            return View(orderListVM);
        }

        public IActionResult Details(int id)
        {
            OrderVM = new OrderVM()
            {
                OrderHeader = _orderHeaderRepo.Find(id),
                OrderDetail = _orderDetailRepo.GetAll(u => u.OrderHeaderId == id, includeProperties: "Product")
            };

            return View(OrderVM);
        }
    }
}
