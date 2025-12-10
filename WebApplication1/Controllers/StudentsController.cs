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

        // GET: /Students or /Students/Index
        [HttpGet]
        public async Task<IActionResult> Index(int page = 1, int pageSize = 3)
        {
            if (pageSize <= 0) pageSize = 3;
            if (page < 1) page = 1;

            var totalItems = await context.Students.CountAsync();
            var totalPages = (int)System.Math.Ceiling(totalItems / (double)pageSize);
            if (totalPages == 0) totalPages = 1;
            if (page > totalPages) page = totalPages;

            var students = await context.Students
                                        .OrderBy(s => s.Id)
                                        .Skip((page - 1) * pageSize)
                                        .Take(pageSize)
                                        .ToListAsync();

            var vm = new StudentsIndexViewModel
            {
                Students = students,
                Page = page,
                PageSize = pageSize,
                TotalItems = totalItems
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
                return RedirectToAction(nameof(Index));
            }
            return View(student);
        }

        // GET: /Students/Edit/2  
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            if (id <= 0) return NotFound();

            var student = await context.Students.FindAsync(id);
            if (student == null) return NotFound();

            return View(student);
        }

        // POST Edit 
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
                ModelState.AddModelError(nameof(student.Id), "Id must be a positive integer greater than zero.");
                return View(student);
            }

            if (student.Id != originalId)
            {
                var exists = await context.Students.AnyAsync(s => s.Id == student.Id);
                if (exists)
                {
                    ModelState.AddModelError(nameof(student.Id), "A student with this Id already exists.");
                    return View(student);
                }

                using var tx = await context.Database.BeginTransactionAsync();
                var existing = await context.Students.FindAsync(originalId);
                if (existing == null)
                {
                    return NotFound();
                }

                if (existing.Id == student.Id
                    && string.Equals(existing.Name?.Trim(), student.Name?.Trim(), System.StringComparison.Ordinal)
                    && string.Equals(existing.Email?.Trim(), student.Email?.Trim(), System.StringComparison.Ordinal)
                    && string.Equals(existing.Mobileno?.Trim(), student.Mobileno?.Trim(), System.StringComparison.Ordinal))
                {
                    ModelState.AddModelError(string.Empty, "No changes detected. Nothing to save.");
                    return View(student);
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

                return RedirectToAction(nameof(Index));
            }
            else
            {
                var existing = await context.Students.FindAsync(originalId);
                if (existing == null)
                {
                    return NotFound();
                }

                if (string.Equals(existing.Name?.Trim(), student.Name?.Trim(), System.StringComparison.Ordinal)
                    && string.Equals(existing.Email?.Trim(), student.Email?.Trim(), System.StringComparison.Ordinal)
                    && string.Equals(existing.Mobileno?.Trim(), student.Mobileno?.Trim(), System.StringComparison.Ordinal))
                {
                    ModelState.AddModelError(string.Empty, "No changes detected. Nothing to save.");
                    return View(student);
                }

                existing.Name = student.Name;
                existing.Email = student.Email;
                existing.Mobileno = student.Mobileno;

                context.Update(existing);
                await context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }
        }

        // GET: /Students/Details/2
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            if (id <= 0) return NotFound();

            var student = await context.Students.FindAsync(id);
            if (student == null) return NotFound();
            return View(student);
        }

        // GET: /Students/Delete/2
        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            if (id <= 0) return NotFound();

            var student = await context.Students.FindAsync(id);
            if (student == null) return NotFound();
            return View(student);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (id <= 0) return NotFound();

            var student = await context.Students.FindAsync(id);
            if (student == null) return NotFound();

            context.Students.Remove(student);
            await context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAll()
        {
            var allStudents = await context.Students.ToListAsync();
            if (allStudents.Count > 0)
            {
                context.Students.RemoveRange(allStudents);
                await context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
