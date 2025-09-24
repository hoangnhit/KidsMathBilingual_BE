using System;
using System.Collections.Generic;

namespace Swd392.Api.Infrastructure.Database.Entities;

public partial class Progress
{
    public int progress_id { get; set; }

    public Guid student_id { get; set; }

    public int course_id { get; set; }

    public int topic_id { get; set; }

    public string status { get; set; } = null!;

    public decimal? score { get; set; }

    public int attempts { get; set; }

    public DateTime? started_at { get; set; }

    public DateTime? completed_at { get; set; }

    public DateTime created_at { get; set; }

    public DateTime updated_at { get; set; }

    public virtual Course course { get; set; } = null!;

    public virtual Student student { get; set; } = null!;

    public virtual Topic topic { get; set; } = null!;
}
