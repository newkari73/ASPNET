using System.ComponentModel.DataAnnotations;

namespace AspNetBbs.Models;

public class LoginViewModel
{
    [Required(ErrorMessage = "아이디를 입력해 주세요.")]
    [StringLength(50)]
    [Display(Name = "아이디")]
    public string UserID { get; set; } = string.Empty;

    [Required(ErrorMessage = "비밀번호를 입력해 주세요.")]
    [DataType(DataType.Password)]
    [Display(Name = "비밀번호")]
    public string UserPWD { get; set; } = string.Empty;
}

public class RegisterViewModel
{
    [Required(ErrorMessage = "아이디를 입력해 주세요.")]
    [StringLength(50, MinimumLength = 4, ErrorMessage = "아이디는 4~50자로 입력해 주세요.")]
    [Display(Name = "아이디")]
    public string UserID { get; set; } = string.Empty;

    [Required(ErrorMessage = "이름을 입력해 주세요.")]
    [StringLength(50, ErrorMessage = "이름은 50자 이내로 입력해 주세요.")]
    [Display(Name = "이름")]
    public string UserName { get; set; } = string.Empty;

    [Required(ErrorMessage = "비밀번호를 입력해 주세요.")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "비밀번호는 6자 이상 입력해 주세요.")]
    [DataType(DataType.Password)]
    [Display(Name = "비밀번호")]
    public string UserPWD { get; set; } = string.Empty;
}