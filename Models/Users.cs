using System;
using System.Collections.Generic;

namespace ChoThueQuanAo.Models
{
    public class User
    {
        public int Id { get; set; }
        public string FullName { get; set; } = "";
        public string Email { get; set; } = "";
        public string? Phone { get; set; }
        public string? Address { get; set; }
        public string PasswordHash { get; set; } = "";
        public string Role { get; set; } = "Customer";
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public ICollection<RentalContract>? CustomerContracts { get; set; }
        public ICollection<RentalContract>? StaffContracts { get; set; }
    }
}