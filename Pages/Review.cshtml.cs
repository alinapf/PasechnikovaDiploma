using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using VeterinaryClinic.Models;
using VeterinaryClinic.Services;

namespace VeterinaryClinic.Pages
{
    public class ReviewModel : PageModel
    {
        private readonly VeterinaryClinicContext _context;
        private readonly LogService _logService;

        public ReviewModel(VeterinaryClinicContext context, LogService logService)
        {
            _context = context;
            _logService = logService;
        }

        public List<ReviewViewModel> Reviews { get; set; }

        [BindProperty]
        public int EditReviewId { get; set; }

        [BindProperty]
        public string EditComment { get; set; }

        public async Task OnGetAsync()
        {
            await LoadReviews();
        }

        public async Task<IActionResult> OnPostApproveAsync(int id)
        {
            var review = await _context.Reviews.FindAsync(id);
            if (review != null)
            {
                review.Status = "Одобрено";
                await _context.SaveChangesAsync();
                /*await _logService.LogAction(,
                $"Одобрен отзыв ID: {id} от клиента {review.Client?.Name} " +
                $"для врача {review.Doctor?.Name}");*/
            }
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostEditAsync(int id)
        {
            var review = await _context.Reviews.FindAsync(id);
            if (review != null)
            {
                EditReviewId = review.ReviewId;
                EditComment = review.Comment;
            }
            await LoadReviews();
            return Page();
        }

        public async Task<IActionResult> OnPostUpdateAsync()
        {
            var review = await _context.Reviews.FindAsync(EditReviewId);
            if (review != null)
            {
                review.Comment = EditComment;
                await _context.SaveChangesAsync();
            }
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostCancelEditAsync()
        {
            return RedirectToPage();
        }

        private async Task LoadReviews()
        {
            Reviews = await _context.Reviews
                .Include(r => r.Client)
                .Include(r => r.Doctor)
                .Where(r => r.Status == "На модерации")
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new ReviewViewModel
                {
                    ReviewId = r.ReviewId,
                    ClientName = r.Client.Name,
                    DoctorName = r.Doctor.Name,
                    Comment = r.Comment,
                    Rating = r.Rating,
                    CreatedAt = r.CreatedAt,
                    Status = r.Status,
                    IsEditable = r.ReviewId == EditReviewId
                })
                .ToListAsync();
        }
    }
}
