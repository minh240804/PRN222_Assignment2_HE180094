using System;
using System.Collections.Generic;

namespace Assignment2.DataAccess.Models;

public partial class SystemAccount
{
    public short AccountId { get; set; }

    public string? AccountName { get; set; }

    public string? AccountEmail { get; set; }

    public int? AccountRole { get; set; }

    public string? AccountPassword { get; set; }

    public bool AccountStatus { get; set; } = true;

    public virtual ICollection<NewsArticle> NewsArticles { get; set; } = new List<NewsArticle>();
    
    public virtual ICollection<Comment> Comments { get; set; } = new List<Comment>();
}
