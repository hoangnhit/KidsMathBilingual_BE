using System;
using System.Collections.Generic;

namespace Swd392.Api.Infrastructure.Database.Entities;

public partial class Admin
{
    public Guid admin_id { get; set; }

    public string username { get; set; } = null!;

    public string? email { get; set; }

    public string password_hash { get; set; } = null!;

    public string role { get; set; } = null!;

    public DateTime created_at { get; set; }

    public DateTime updated_at { get; set; }

    public virtual ICollection<Course> Courses { get; set; } = new List<Course>();
}
