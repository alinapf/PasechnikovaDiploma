using VeterinaryClinic.Models;

namespace VeterinaryClinic.Services
{
    public class LogService
    {
        private readonly VeterinaryClinicContext _context;

        public LogService(VeterinaryClinicContext context)
        {
            _context = context;
        }

        public async Task LogAction(int? userId, string action)
        {
            var log = new Log
            {
                UserId = userId,
                Action = action,
                Timestamp = DateTime.Now
            };

            _context.Logs.Add(log);
            await _context.SaveChangesAsync();
        }
    }
}
