using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using WebApplication1.Data;
using WebApplication1.Models;

namespace WebApplication1.Controllers
{
    public class StudentsController : Controller
    {
        private readonly ApplicationDbContext context;

        public StudentsController (ApplicationDbContext context)
        {
            this.context = context; 
        }
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var student = await context.Students.ToListAsync();
            return View(student);
        }
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Students student)
        {
            if (ModelState.IsValid)
            {
                context.Students.Add(student);
                await context.SaveChangesAsync();
                return RedirectToAction("Index", "Students");
            }
            return View(student);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var student = await context.Students.FindAsync(id);
            if (student == null)
            {
                return NotFound();
            }
            return View(student);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Students student)
        {
            if (ModelState.IsValid)
            {
                context.Update(student);
                await context.SaveChangesAsync();

                return RedirectToAction("Index", "Students");
            }

            return View(student);
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var student = await context.Students.FindAsync(id);
            if (student == null)
            {
                return NotFound();
            }
            return View(student);
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var student = await context.Students.FindAsync(id);
            if (student == null)
            {
                return NotFound();
            }
            return View(student);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var student = await context.Students.FindAsync(id);
            if (student == null)
            {
                return NotFound();
            }
            context.Students.Remove(student);
            await context.SaveChangesAsync();
            return RedirectToAction("Index", "Students");
        }
    }
} 
