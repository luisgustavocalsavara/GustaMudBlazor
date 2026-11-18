namespace GustaMudBlazor.Models.Entities;

public enum UserRole
{
    Manutencao = 0, // Apenas visualiza as telas
    Operacao = 1,   // Pode criar, editar e excluir receitas, blocos e itens
    Admin = 2       // Acesso total + gerenciamento de usuários
}

public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.Operacao;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
