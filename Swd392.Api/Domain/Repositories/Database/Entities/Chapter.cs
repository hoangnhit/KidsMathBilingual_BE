using System;
using System.Collections.Generic;

namespace Swd392.Api.Infrastructure.Database.Entities;

public partial class Chapter
{
    public int chapter_id { get; set; }

    public string title_en { get; set; } = null!;

    public string? title_local { get; set; }

    public int? position { get; set; }

    public string status { get; set; } = null!;

    public int topic_id { get; set; }

    public DateTime created_at { get; set; }

    public DateTime updated_at { get; set; }

    public virtual Topic topic { get; set; } = null!;
}
