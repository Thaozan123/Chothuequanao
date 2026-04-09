using Microsoft.AspNetCore.Mvc;
using ChoThueQuanAo.Services;
using ChoThueQuanAo.Models;
using ChoThueQuanAo.Data;

namespace ChoThueQuanAo.Controllers
{
    public class RentalContractController : Controller
    {
        private readonly RentalContractService _service;
        private readonly AppDbContext _context;

        public RentalContractController(RentalContractService service, AppDbContext context)
        {
            _service = service;
            _context = context;
        }

        // GET: /RentalContract
        public async Task<IActionResult> Index()
        {
            var data = await _service.GetAllAsync();
            return View(data);
        }
    }
}