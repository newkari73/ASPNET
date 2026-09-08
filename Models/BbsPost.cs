using System.ComponentModel.DataAnnotations;

namespace AspNetBbs.Models;

public class BbsPost
{
    public int ID { get; set; }

    [Required(ErrorMessage = "이름을 입력해 주세요.")]
    [StringLength(50, ErrorMessage = "이름은 50자 이내로 입력해 주세요.")]
    [Display(Name = "작성자")]
    public string UserName { get; set; } = string.Empty;

    [Display(Name = "등록일")]
    public DateTime RegDate { get; set; }

    [Required(ErrorMessage = "제목을 입력해 주세요.")]
    [StringLength(200, ErrorMessage = "제목은 200자 이내로 입력해 주세요.")]
    [Display(Name = "제목")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "내용을 입력해 주세요.")]
    [Display(Name = "내용")]
    public string Contents { get; set; } = string.Empty;

    [Display(Name = "첨부파일")]
    public string? File { get; set; }
}

public class BbsPageViewModel
{
    public IReadOnlyList<BbsPost> Posts { get; init; } = [];
    public int CurrentPage { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}

public class BbsComment
{
    public int ID { get; set; }
    public int BbsID { get; set; }

    [Required(ErrorMessage = "이름을 입력해 주세요.")]
    [StringLength(50, ErrorMessage = "이름은 50자 이내로 입력해 주세요.")]
    [Display(Name = "작성자")]
    public string UserName { get; set; } = string.Empty;

    [Required(ErrorMessage = "댓글 내용을 입력해 주세요.")]
    [StringLength(1000, ErrorMessage = "댓글은 1,000자 이내로 입력해 주세요.")]
    [Display(Name = "댓글")]
    public string Contents { get; set; } = string.Empty;

    public DateTime RegDate { get; set; }
}

public class BbsDetailViewModel
{
    public BbsPost Post { get; init; } = new();
    public IReadOnlyList<BbsComment> Comments { get; init; } = [];
}