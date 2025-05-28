using Domain.Entities;
using System.Collections.Generic;

namespace Application.DTO
{
    public class ProfileDTO
    {
        public string Username { get; set; }
        public string FullName { get; set; }
        public UserRole Role { get; set; }
        public List<Reservation> Reservations { get; set; }
    }
}