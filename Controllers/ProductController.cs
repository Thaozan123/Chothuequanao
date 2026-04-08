using ChoThueQuanAo.Data;
using ChoThueQuanAo.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ChoThueQuanAo.Controllers
{
   public class ProductController : Controller
   {
       private readonly AppDbContext _context;

       public ProductController(AppDbContext context)
       {
           _context = context;
       }

       public async Task<IActionResult> Index()
       {
           var products = await _context.Products
               .Include(p => p.Category)
               .ToListAsync();

           return View(products);
       }

       public IActionResult Create()
       {
           ViewBag.Categories = _context.ProductCategories.ToList();
           return View();
       }

       [HttpPost]
       public async Task<IActionResult> Create(Product product)
       {
           if (ModelState.IsValid)
           {
               _context.Products.Add(product);
               await _context.SaveChangesAsync();
               return RedirectToAction(nameof(Index));
           }

           ViewBag.Categories = _context.ProductCategories.ToList();
           return View(product);
       }
   }
}
