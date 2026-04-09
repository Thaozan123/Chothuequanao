using Microsoft.EntityFrameworkCore;
using ChoThueQuanAo.Data;
using ChoThueQuanAo.Models;

namespace ChoThueQuanAo.Services
{
    public class RentalContractService
    {
        private readonly AppDbContext _db;

        public RentalContractService(AppDbContext db)
        {
            _db = db;
        }

        public async Task<List<RentalContract>> GetAllAsync()
        {
            return await _db.RentalContracts
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }
    }
}