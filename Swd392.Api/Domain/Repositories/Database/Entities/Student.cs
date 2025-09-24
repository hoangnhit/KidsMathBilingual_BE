using System;
using System.Collections.Generic;

namespace Swd392.Api.Infrastructure.Database.Entities;

public partial class Student
{
    public Guid student_id { get; set; }

    public string username { get; set; } = null!;

    public string password_hash { get; set; } = null!;

    public string full_name { get; set; } = null!;

    public DateOnly? birthday { get; set; }

    public int? grade { get; set; }

    public Guid? parent_id { get; set; }

    public string role { get; set; } = null!;

    public DateTime created_at { get; set; }

    public DateTime updated_at { get; set; }

    public virtual ICollection<Progress> Progresses { get; set; } = new List<Progress>();

    public virtual ICollection<StudentBadge> StudentBadges { get; set; } = new List<StudentBadge>();

    public virtual ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();

    public virtual Parent? parent { get; set; }
}
