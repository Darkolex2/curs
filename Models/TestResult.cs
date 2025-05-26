using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Workforce.Models
{
    public class TestResult
    {
        public int Id { get; set; }

        [Required]
        public int CourseId { get; set; }

        [Required]
        public int StudentId { get; set; }

        [Required]
        public double Score { get; set; }

        [ForeignKey("CourseId")]
        public Course Course { get; set; } = null!;

        [ForeignKey("StudentId")]
        public Student Student { get; set; } = null!;
    }
}
