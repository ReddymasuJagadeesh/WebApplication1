using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using WebApplication1.Data;
using WebApplication1.Models;

namespace WebApplication1.Controllers
{
    public class StudentsController : Controller
    {
        private readonly ApplicationDbContext context;

        public StudentsController(ApplicationDbContext context)
        {
            this.context = context;
        }

        // Shows a paged list of students. 'page' is 1-based. 'pageSize' controls how many items per page.
        [HttpGet]
        public async Task<IActionResult> Index(int page = 1, int pageSize = 3)
        {
            var allowedPageSizes = new[] { 2, 3, 5, 10 };

            if (pageSize <= 0 || !allowedPageSizes.Contains(pageSize))
            {
                pageSize = 3;
            }

            if (page < 1) page = 1;

            var totalItems = await context.Students.CountAsync();
            var totalPages = (int)System.Math.Ceiling(totalItems / (double)pageSize);
            if (totalPages == 0) totalPages = 1;
            if (page > totalPages) page = totalPages;

            var students = await context.Students
                                        .OrderBy(s => s.Id)           // ensure Id order
                                        .Skip((page - 1) * pageSize)
                                        .Take(pageSize)
                                        .ToListAsync();

            var vm = new StudentsIndexViewModel
            {
                Students = students,
                Page = page,
                PageSize = pageSize,
                TotalItems = totalItems,
                AllowedPageSizes = allowedPageSizes
            };

            return View(vm);
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
        public async Task<IActionResult> Edit([FromForm] int originalId, Students student)
        {
            if (originalId <= 0)
            {
                originalId = student?.Id ?? 0;
            }

            if (student == null || originalId <= 0)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                return View(student);
            }

            if (student.Id <= 0)
            {
                ModelState.AddModelError("Id", "Id must be a positive integer greater than zero.");
                return View(student);
            }

            if (student.Id != originalId)
            {
                var exists = await context.Students.AnyAsync(s => s.Id == student.Id);
                if (exists)
                {
                    ModelState.AddModelError("Id", "A student with this Id already exists.");
                    return View(student);
                }

                using var tx = await context.Database.BeginTransactionAsync();
                var existing = await context.Students.FindAsync(originalId);
                if (existing == null)
                {
                    return NotFound();
                }

                var newStudent = new Students
                {
                    Id = student.Id,
                    Name = student.Name,
                    Email = student.Email,
                    Mobileno = student.Mobileno
                };

                context.Students.Add(newStudent);
                context.Students.Remove(existing);

                await context.SaveChangesAsync();
                await tx.CommitAsync();

                return RedirectToAction("Index", "Students");
            }
            else
            {
                var existing = await context.Students.FindAsync(originalId);
                if (existing == null)
                {
                    return NotFound();
                }

                existing.Name = student.Name;
                existing.Email = student.Email;
                existing.Mobileno = student.Mobileno;

                context.Update(existing);
                await context.SaveChangesAsync();

                return RedirectToAction("Index", "Students");
            }
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