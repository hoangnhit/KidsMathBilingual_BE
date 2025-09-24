using System;
using System.Collections.Generic;

namespace Swd392.Api.Infrastructure.Database.Entities;

public partial class Topic
{
    public int topic_id { get; set; }

    public string title_en { get; set; } = null!;

    public string? title_local { get; set; }

    public int? position { get; set; }

    public bool is_bilingual { get; set; }

    public string status { get; set; } = null!;

    public int course_id { get; set; }

    public DateTime created_at { get; set; }

    public DateTime updated_at { get; set; }

    public virtual ICollection<Chapter> Chapters { get; set; } = new List<Chapter>();

    public virtual ICollection<Progress> Progresses { get; set; } = new List<Progress>();

    public virtual ICollection<Quiz> Quizzes { get; set; } = new List<Quiz>();

    public virtual Course course { get; set; } = null!;
}
