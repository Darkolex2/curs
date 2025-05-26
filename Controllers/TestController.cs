using Microsoft.AspNetCore.Mvc;
using Workforce.Data;
using Workforce.Models;
using System.Threading.Tasks;

namespace Workforce.Controllers
{
    public class TestController : Controller
    {
        private readonly SchoolContext _context;

        public TestController(SchoolContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<IActionResult> SaveResults(int courseId, int studentId, int score)
        {
            var course = await _context.Courses.FindAsync(courseId);
            var student = await _context.Students.FindAsync(studentId);

            if (course == null || student == null)
            {
                return NotFound();
            }

            var testResult = new TestResult
            {
                CourseId = courseId,
                StudentId = studentId,
                Score = score
            };

            _context.TestResults.Add(testResult);
            await _context.SaveChangesAsync();

            return RedirectToAction("Index", "Home");
        }
    }
}
