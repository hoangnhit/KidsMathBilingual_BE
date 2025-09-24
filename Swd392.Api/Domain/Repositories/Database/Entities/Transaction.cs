using System;
using System.Collections.Generic;

namespace Swd392.Api.Infrastructure.Database.Entities;

public partial class Transaction
{
    public int transaction_id { get; set; }

    public Guid? parent_id { get; set; }

    public Guid? student_id { get; set; }

    public int course_id { get; set; }

    public decimal amount { get; set; }

    public string currency { get; set; } = null!;

    public string? method { get; set; }

    public string status { get; set; } = null!;

    public string? external_ref { get; set; }

    public DateTime created_at { get; set; }

    public DateTime updated_at { get; set; }

    public virtual Course course { get; set; } = null!;

    public virtual Parent? parent { get; set; }

    public virtual Student? student { get; set; }
}
