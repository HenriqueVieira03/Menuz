using ProjetoIntegradorMenuz.Models;

namespace ProjetoIntegradorMenuz.ViewModels
{
    /// <summary>Perfil escolhido no cadastro: valor enviado à API e texto mostrado na tela.</summary>
    public record RoleOption(string Value, string Label)
    {
        public static readonly RoleOption Kitchen = new(StaffRoles.Kitchen, "Cozinha");
        public static readonly RoleOption Manager = new(StaffRoles.Manager, "Gerência");
        public static readonly IReadOnlyList<RoleOption> All = new[] { Kitchen, Manager };

        public static RoleOption From(string? role) => role == StaffRoles.Manager ? Manager : Kitchen;
    }

    /// <summary>Card de um usuário na aba "Usuários" da gerência.</summary>
    public class ManagerUserViewModel
    {
        public ManagerUserViewModel(UserAdmin dto)
        {
            Id = dto.Id;
            Title = dto.DisplayName;
            Subtitle = $"@{dto.Username}";
            RoleText = RoleOption.From(dto.Role).Label;
            IsActive = dto.IsActive;
            IsManager = dto.Role == StaffRoles.Manager;
        }

        public int Id { get; }
        public string Title { get; }
        public string Subtitle { get; }
        public string RoleText { get; }
        public bool IsActive { get; }
        public bool IsInactive => !IsActive;
        public bool IsManager { get; }
        public string StatusText => IsActive ? "Ativo" : "Desativado";
    }
}
