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
            var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var review = await _context.Reviews
                .Include(r => r.Client)
                .Include(r => r.Doctor)
                .FirstOrDefaultAsync(r => r.ReviewId == id);

            if (review != null)
            {
                review.Status = "Одобрено";
                await _context.SaveChangesAsync();

                await _logService.LogAction(
                    currentUserId,
                    $"Одобрен отзыв ID: {review.ReviewId} | " +
                    $"Клиент: {review.Client?.Name} | " +
                    $"Врач: {review.Doctor?.Name} | " +
                    $"Рейтинг: {review.Rating}/5");
            }
            else
            {
                await _logService.LogAction(
                    currentUserId,
                    $"Попытка одобрения несуществующего отзыва ID: {id}");
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostEditAsync(int id)
        {
            var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var review = await _context.Reviews
                .Include(r => r.Client)
                .Include(r => r.Doctor)
                .FirstOrDefaultAsync(r => r.ReviewId == id);

            if (review != null)
            {
                EditReviewId = review.ReviewId;
                EditComment = review.Comment;

                await _logService.LogAction(
                    currentUserId,
                    $"Начато редактирование отзыва ID: {review.ReviewId} | " +
                    $"Текущий текст: {review.Comment.Truncate(50)}");
            }

            await LoadReviews();
            return Page();
        }

        public async Task<IActionResult> OnPostUpdateAsync()
        {
            var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var review = await _context.Reviews
                .Include(r => r.Client)
                .Include(r => r.Doctor)
                .FirstOrDefaultAsync(r => r.ReviewId == EditReviewId);

            if (review != null)
            {
                var oldComment = review.Comment;
                review.Comment = EditComment;
                await _context.SaveChangesAsync();

                await _logService.LogAction(
                    currentUserId,
                    $"Изменен отзыв ID: {review.ReviewId} | " +
                    $"Старый текст: {oldComment.Truncate(50)} | " +
                    $"Новый текст: {EditComment.Truncate(50)}");
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
    public static class StringExtensions
    {
        public static string Truncate(this string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value)) return value;
            return value.Length <= maxLength ? value : value.Substring(0, maxLength) + "...";
        }
    }
}
