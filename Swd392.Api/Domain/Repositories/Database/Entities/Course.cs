using System;
using System.Collections.Generic;

namespace Swd392.Api.Infrastructure.Database.Entities;

public partial class Course
{
    public int course_id { get; set; }

    public string title_en { get; set; } = null!;

    public string? title_local { get; set; }

    public int? grade_level { get; set; }

    public decimal price { get; set; }

    public string status { get; set; } = null!;

    public DateOnly? start_date { get; set; }

    public DateOnly? end_date { get; set; }

    public DateTime created_at { get; set; }

    public DateTime updated_at { get; set; }

    public Guid? creator_id { get; set; }

    public virtual ICollection<Progress> Progresses { get; set; } = new List<Progress>();

    public virtual ICollection<Quiz> Quizzes { get; set; } = new List<Quiz>();

    public virtual ICollection<Topic> Topics { get; set; } = new List<Topic>();

    public virtual ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();

    public virtual Admin? creator { get; set; }
}
