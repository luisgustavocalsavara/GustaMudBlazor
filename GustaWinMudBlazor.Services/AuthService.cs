using GustaMudBlazor.Models.Entities;
using GustaMudBlazor.Models.Interfaces;
using Microsoft.EntityFrameworkCore;
using GustaMudBlazor.Services.Data;

namespace GustaMudBlazor.Services;

public class AuthService(IDbContextFactory<AppDbContext> dbFactory) : IAuthService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory = dbFactory;

    public User? CurrentUser { get; private set; }
    public bool IsAuthenticated => CurrentUser is not null;

    public event Action? AuthStateChanged;

    public async Task<(bool Success, string Message)> LoginAsync(string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            return (false, "Informe o usuário e a senha.");

        await using var db = await _dbFactory.CreateDbContextAsync();
        var hash = AppDbContext.HashPassword(password);
        var normalizedUser = username.Trim().ToLower();

        var user = await db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Username.ToLower() == normalizedUser && u.PasswordHash == hash);

        if (user is null)
            return (false, "Usuário ou senha inválidos.");

        CurrentUser = user;
        AuthStateChanged?.Invoke();
        return (true, $"Bem-vindo, {user.FullName}!");
    }

    public async Task<(bool Success, string Message)> RegisterUserAsync(string username, string password, string fullName, UserRole role)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(fullName))
            return (false, "Preencha todos os campos obrigatórios.");

        if (password.Length < 8)
            return (false, "A senha deve ter pelo menos 8 caracteres.");

        if (!Enum.IsDefined(role))
            return (false, "Selecione um nível de permissão válido.");

        await using var db = await _dbFactory.CreateDbContextAsync();
        var normalized = username.Trim().ToLower();

        var exists = await db.Users.AnyAsync(u => u.Username.ToLower() == normalized);
        if (exists)
            return (false, "Este nome de usuário já está cadastrado.");

        var newUser = new User
        {
            Username = username.Trim(),
            PasswordHash = AppDbContext.HashPassword(password),
            FullName = fullName.Trim(),
            Role = role,
            CreatedAt = DateTime.Now
        };

        db.Users.Add(newUser);
        await db.SaveChangesAsync();
        return (true, "Usuário cadastrado com sucesso!");
    }

    public async Task<(bool Success, string Message)> ChangeCurrentUserPasswordAsync(string currentPassword, string newPassword)
    {
        if (CurrentUser is null)
            return (false, "Nenhum usuário está conectado.");

        if (string.IsNullOrWhiteSpace(currentPassword) || string.IsNullOrWhiteSpace(newPassword))
            return (false, "Informe a senha atual e a nova senha.");

        if (newPassword.Length < 3)
            return (false, "A nova senha deve ter pelo menos 8 caracteres.");

        if (currentPassword == newPassword)
            return (false, "A nova senha deve ser diferente da senha atual.");

        await using var db = await _dbFactory.CreateDbContextAsync();
        var user = await db.Users.FindAsync(CurrentUser.Id);
        if (user is null)
            return (false, "O usuário conectado não foi encontrado.");

        if (user.PasswordHash != AppDbContext.HashPassword(currentPassword))
            return (false, "A senha atual está incorreta.");

        user.PasswordHash = AppDbContext.HashPassword(newPassword);
        await db.SaveChangesAsync();
        CurrentUser.PasswordHash = user.PasswordHash;
        return (true, "Senha alterada com sucesso.");
    }

    public async Task<(bool Success, string Message)> ChangeUserRoleAsync(int userId, UserRole role)
    {
        if (CurrentUser is null || CurrentUser.Role != UserRole.Admin)
            return (false, "Somente um administrador pode alterar permissões.");

        if (!Enum.IsDefined(role))
            return (false, "Selecione um nível de permissão válido.");

        if (CurrentUser.Id == userId && role != UserRole.Admin)
            return (false, "Não é possível remover sua própria permissão de administrador.");

        await using var db = await _dbFactory.CreateDbContextAsync();
        var user = await db.Users.FindAsync(userId);
        if (user is null)
            return (false, "Usuário não encontrado.");

        if (user.Role == UserRole.Admin && role != UserRole.Admin)
        {
            var otherAdminsExist = await db.Users.AnyAsync(u => u.Id != userId && u.Role == UserRole.Admin);
            if (!otherAdminsExist)
                return (false, "O sistema precisa manter pelo menos um administrador.");
        }

        user.Role = role;
        await db.SaveChangesAsync();
        if (CurrentUser.Id == userId)
        {
            CurrentUser.Role = role;
            AuthStateChanged?.Invoke();
        }

        return (true, "Permissão atualizada com sucesso.");
    }

    public async Task<List<User>> GetAllUsersAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.Users.AsNoTracking().OrderBy(u => u.Username).ToListAsync();
    }

    public async Task DeleteUserAsync(int userId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var user = await db.Users.FindAsync(userId);
        if (user is not null && user.Username.ToLower() != "admin")
        {
            db.Users.Remove(user);
            await db.SaveChangesAsync();
        }
    }

    public void Logout()
    {
        CurrentUser = null;
        AuthStateChanged?.Invoke();
    }

    public bool HasRole(params UserRole[] allowedRoles)
    {
        if (CurrentUser is null) return false;
        return allowedRoles.Contains(CurrentUser.Role);
    }
}
