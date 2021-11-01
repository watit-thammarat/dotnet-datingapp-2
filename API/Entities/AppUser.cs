using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Identity;

namespace API.Entities
{
    public class AppUser : IdentityUser<int>
    {
        public DateTime DateOfBirth { get; set; }
        public string KnownAs { get; set; }
        public DateTime Created { get; set; } = DateTime.Now;
        public DateTime LastActive { get; set; } = DateTime.Now;
        public string Gender { get; set; }
        public string Introduction { get; set; }
        public string LookingFor { get; set; }
        public string Interests { get; set; }
        public string City { get; set; }
        public string Country { get; set; }
        public ICollection<Photo> Photos { get; set; } = new List<Photo>();
        public ICollection<UserLike> LikedByUsers { get; set; } = new List<UserLike>();
        public ICollection<UserLike> LikedUsers { get; set; } = new List<UserLike>();
        public ICollection<Message> MessageSent { get; set; } = new List<Message>();
        public ICollection<Message> MessageReceived { get; set; } = new List<Message>();
        public ICollection<AppUserRole> UserRoles { get; set; } = new List<AppUserRole>();
    }
}