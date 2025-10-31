using System;

namespace Assignment2.DataAccess.Models;

public partial class Comment
{
    public int CommentId { get; set; }

    public string ArticleId { get; set; } = null!;

    public short AccountId { get; set; }

    public string Content { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public bool IsDeleted { get; set; }
    
    public short? DeletedBy { get; set; }
    
    public DateTime? DeletedAt { get; set; }

    public virtual NewsArticle Article { get; set; } = null!;

    public virtual SystemAccount Account { get; set; } = null!;
    
    public virtual SystemAccount? DeletedByAccount { get; set; }
}
