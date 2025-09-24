using System;
using System.Collections.Generic;

namespace Swd392.Api.Infrastructure.Database.Entities;

public partial class Badge
{
    public int badge_id { get; set; }

    public string name { get; set; } = null!;

    public string? description { get; set; }

    public string? criteria { get; set; }

    public string status { get; set; } = null!;

    public DateTime created_at { get; set; }

    public DateTime updated_at { get; set; }

    public virtual ICollection<StudentBadge> StudentBadges { get; set; } = new List<StudentBadge>();
}
