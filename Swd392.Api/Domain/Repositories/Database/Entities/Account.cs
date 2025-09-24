using System;
using System.Collections.Generic;

namespace Swd392.Api.Infrastructure.Database.Entities;

public partial class Account
{
    public Guid account_id { get; set; }

    public string username { get; set; } = null!;

    public string fullname { get; set; } = null!;

    public string email { get; set; } = null!;

    public string password_hash { get; set; } = null!;

    public string role { get; set; } = null!;

    public DateTime created_at { get; set; }

    public DateTime updated_at { get; set; }
}
