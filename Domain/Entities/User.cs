using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string Password { get; set; } 
        public string FullName { get; set; }

        public UserRole Role { get; set; }

        public ICollection<Reservation> Reservations { get; set; }
    }

    public enum UserRole
    {
        Admin,
        User
    }

}
