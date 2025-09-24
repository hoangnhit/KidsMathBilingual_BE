using System;
using System.Collections.Generic;

namespace Swd392.Api.Infrastructure.Database.Entities;

public partial class Quiz
{
    public int quiz_id { get; set; }

    public int topic_id { get; set; }

    public int course_id { get; set; }

    public string title_en { get; set; } = null!;

    public string? title_local { get; set; }

    public decimal passing_score { get; set; }

    public string status { get; set; } = null!;

    public DateTime created_at { get; set; }

    public DateTime updated_at { get; set; }

    public virtual ICollection<Question> Questions { get; set; } = new List<Question>();

    public virtual Course course { get; set; } = null!;

    public virtual Topic topic { get; set; } = null!;
}
