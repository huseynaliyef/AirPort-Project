using Business.Abstractions;
using Business.DTOs;
using Business.DTOs.Viewmodels;
using Data.Entities;
using Microsoft.AspNetCore.Mvc;

namespace Presentation.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IPortRepository _portRepository;

        public HomeController(ILogger<HomeController> logger, IPortRepository portRepository)
        {
            _logger = logger;
            _portRepository = portRepository;
        }

        public async Task<IActionResult> Index(PortSearchDTO model)
        {
            var IndexModel = new IndexViewModel();
            if(model.effectiveDate == default(DateTime))
            {
                model = new PortSearchDTO { effectiveDate = DateTime.Now};
                IndexModel.SearchedPorts = await _portRepository.GetPorts(model);
                IndexModel.SearchDate = model.effectiveDate;
            }
            else
            {
                IndexModel.SearchedPorts = await _portRepository.GetPorts(model);
                IndexModel.SearchDate = model.effectiveDate;
            }
            return View(IndexModel);
        }

        public async Task<IActionResult> Add()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Add(PortAddDTO model)
        {
            if (ModelState.IsValid)
            {
                await _portRepository.AddPort(model);
            }
            return RedirectToAction("Index");
        }

        [HttpGet("Edit/{Identifier}/{searchedDate}")]
        public IActionResult Edit(Guid Identifier, DateTime searchedDate)
        {
            var PortModel = new EditViewModel { Identifier = Identifier, VTBegin = searchedDate};
            return View(PortModel);
        }

        [HttpPost("Edit/{Identifier}/{searchedDate}")]
        public async Task<IActionResult> Edit(PortEditDTO model)
        {
            if(ModelState.IsValid) 
            {
                await _portRepository.EditPort(model);
            
            }
            return RedirectToAction("Index");
        }


        [HttpGet("Decommission/{Identifier}/{searchedDate}")]
        public async Task<IActionResult> Decommission(Guid Identifier, DateTime searchedDate)
        {
            var portModel = new DecommissionViewModel { Identifier = Identifier, searchedDate = searchedDate};
            return View(portModel);
        }

        [HttpPost("Decommission/{Identifier}/{searchedDate}")]
        public async Task<IActionResult> Decommission(PortDecommissionDTO model)
        {
            await _portRepository.DecommissionPort(model);
            return RedirectToAction("Index");
        }



    }
}
