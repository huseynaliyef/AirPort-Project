using Business.Abstractions;
using Business.DTOs;
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

        public async Task<IActionResult> Index()
        {
            return View();
        }

        public async Task<IActionResult> Search(PortSearchDTO model)
        {
            var SearchedPort = new List<PortOne>();
            if (model.State == States.BASELINE)
            {
                SearchedPort = await _portRepository.GetPortByBaseLine(model);
            }
            else if(model.State == States.SNAPSHOT)
            {
                SearchedPort = await _portRepository.GetPortBySnapShot(model);
            }
            return View(SearchedPort);
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

        public async Task<IActionResult> Edit(int Id)
        {
            var Port = await _portRepository.GetPortById(Id);
            return View(Port);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(PortEditDTO model)
        {
            if(ModelState.IsValid) 
            {
                await _portRepository.EditPort(model);
            
            }
            return RedirectToAction("Index");
        }


        public async Task<IActionResult> Decommission(int Id)
        {
            var Port = await _portRepository.GetPortById(Id);
            return View(Port);
        }

        [HttpPost]
        public async Task<IActionResult> Decommission(PortDecommissionDTO model)
        {
            await _portRepository.DecommissionPort(model);
            return RedirectToAction("Index");
        }



    }
}
