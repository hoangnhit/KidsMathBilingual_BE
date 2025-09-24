using System;
using System.Collections.Generic;

namespace Swd392.Api.Infrastructure.Database.Entities;

public partial class StudentBadge
{
    public int student_badge_id { get; set; }

    public Guid student_id { get; set; }

    public int badge_id { get; set; }

    public DateTime earned_at { get; set; }

    public string? source { get; set; }

    public DateTime created_at { get; set; }

    public DateTime updated_at { get; set; }

    public virtual Badge badge { get; set; } = null!;

    public virtual Student student { get; set; } = null!;
}
