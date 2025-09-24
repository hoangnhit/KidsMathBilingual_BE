using System;
using System.Collections.Generic;

namespace Swd392.Api.Infrastructure.Database.Entities;

public partial class Question
{
    public int question_id { get; set; }

    public int quiz_id { get; set; }

    public string content_en { get; set; } = null!;

    public string? content_local { get; set; }

    public string? options_json { get; set; }

    public string? correct_key { get; set; }

    public string? difficulty { get; set; }

    public DateTime created_at { get; set; }

    public DateTime updated_at { get; set; }

    public virtual Quiz quiz { get; set; } = null!;
}
