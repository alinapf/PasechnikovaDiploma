using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

using VeterinaryClinic.Models;
using VeterinaryClinic.Services;

namespace VeterinaryClinic.Pages
{
    public class AddUserModel : PageModel
    {
        private readonly VeterinaryClinicContext _context;
        private readonly LogService _logService;
        private readonly IPasswordHasher<User> _passwordHasher;

        public AddUserModel(VeterinaryClinicContext context, LogService logService, IPasswordHasher<User> passwordHasher)
        {
            _context = context;
            _logService = logService;
            _passwordHasher = passwordHasher;
        }

        [BindProperty]
        public User User { get; set; } = new User { UserId = 0 };

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            try
            {
                if (id == null)
                {
                    // Режим создания нового пользователя - пользователь ещё не существует
                    await _logService.LogAction(null, "Открыта страница создания нового пользователя");
                    return Page();
                }

                // Режим редактирования существующего пользователя
                User = await _context.Users.FindAsync(id);

                if (User == null)
                {
                    await _logService.LogAction(null, $"Попытка редактирования несуществующего пользователя ID: {id}");
                    return NotFound();
                }

                await _logService.LogAction(User.UserId, $"Открыта страница редактирования пользователя ID: {id}");

                var logMessage = $"Редактирование пользователя: " +
                                $"Имя: {User.Username}, " +
                                $"Email: {User.Email}, " +
                                $"Роль: {User.Role}";

                await _logService.LogAction(User.UserId, logMessage);

                return Page();
            }
            catch (Exception ex)
            {
                await _logService.LogAction(null, $"Ошибка при открытии страницы пользователя: {ex.Message}");
                throw;
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {

            

            try
            {
                if (User.UserId == 0)
                {
                    // Хеширование пароля перед сохранением
                    if (!string.IsNullOrEmpty(User.Password))
                    {
                        User.Password = _passwordHasher.HashPassword(User, User.Password);
                    }

                    _context.Users.Add(User);
                    await _context.SaveChangesAsync();

                    var logMessage = $"Создан новый пользователь: " +
                                   $"ID: {User.UserId}, " +
                                   $"Username: {User.Username}, " +
                                   $"Email: {User.Email}, " +
                                   $"Role: {User.Role}";

                    await _logService.LogAction(User.UserId, logMessage);
                }
                else
                {
                    var userToUpdate = await _context.Users.FindAsync(User.UserId);

                    if (userToUpdate == null)
                    {
                        await _logService.LogAction(User.UserId,
                            $"Попытка обновления несуществующего пользователя ID: {User.UserId}");
                        return NotFound();
                    }

                    var changes = new List<string>();

                    // Логируем изменения полей
                    if (userToUpdate.Username != User.Username)
                    {
                        changes.Add($"Username: {userToUpdate.Username} → {User.Username}");
                        userToUpdate.Username = User.Username;
                    }

                    if (userToUpdate.Email != User.Email)
                    {
                        changes.Add($"Email: {userToUpdate.Email} → {User.Email}");
                        userToUpdate.Email = User.Email;
                    }

                    if (userToUpdate.Role != User.Role)
                    {
                        changes.Add($"Role: {userToUpdate.Role} → {User.Role}");
                        userToUpdate.Role = User.Role;
                    }

                    // Обновляем пароль только если он был указан
                    if (!string.IsNullOrEmpty(User.Password))
                    {
                        changes.Add("Пароль изменен");
                        userToUpdate.Password = _passwordHasher.HashPassword(userToUpdate, User.Password);
                    }

                    if (changes.Any())
                    {
                        await _context.SaveChangesAsync();

                        var logMessage = $"Обновлен пользователь ID: {User.UserId}. " +
                                       $"Изменения: {string.Join(", ", changes)}";

                        await _logService.LogAction(User.UserId, logMessage);
                    }
                    else
                    {
                        await _logService.LogAction(User.UserId,
                            $"Попытка обновления пользователя ID: {User.UserId} без изменений");
                    }
                }

                return RedirectToPage("./Users");
            }
            catch (DbUpdateException ex)
            {
                await _logService.LogAction(User.UserId,
                    $"Ошибка при сохранении пользователя: {ex.Message}");

                ModelState.AddModelError("", "Не удалось сохранить изменения. " +
                    (ex.InnerException?.Message ?? ex.Message));
                return Page();
            }
            catch (Exception ex)
            {
                await _logService.LogAction(User.UserId,
                    $"Неожиданная ошибка при работе с пользователем: {ex.Message}");
                throw;
            }
        }
    }
}
