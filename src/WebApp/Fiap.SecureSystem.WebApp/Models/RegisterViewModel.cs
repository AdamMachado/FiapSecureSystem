using System.ComponentModel.DataAnnotations;

namespace Fiap.SecureSystem.WebApp.Models;

public sealed class RegisterViewModel
{
    [Required(ErrorMessage = "Informe seu nome.")]
    [Display(Name = "Nome")]
    [StringLength(200, MinimumLength = 2, ErrorMessage = "Informe um nome entre 2 e 200 caracteres.")]
    public string DisplayName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe o e-mail.")]
    [EmailAddress(ErrorMessage = "Informe um e-mail valido.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe a senha.")]
    [DataType(DataType.Password)]
    [MinLength(8, ErrorMessage = "A senha deve ter pelo menos 8 caracteres.")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Confirme a senha.")]
    [DataType(DataType.Password)]
    [Display(Name = "Confirmar senha")]
    [Compare(nameof(Password), ErrorMessage = "As senhas precisam ser iguais.")]
    public string ConfirmPassword { get; set; } = string.Empty;

    public string? ErrorMessage { get; set; }
    public string? ReturnUrl { get; set; }
}
