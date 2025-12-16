using DocTask.Core.Interfaces.Repositories;
using DocTask.Core.Models;
using Microsoft.EntityFrameworkCore;
using DocTask.Core.Dtos.Users;

namespace DocTask.Data.Repositories;

public class UserRepository : IUserRepository
{
    private ApplicationDbContext _context;

    public UserRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async System.Threading.Tasks.Task<User?> GetByUserNameAsync(string username)
    {
        return await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
    }

    public async System.Threading.Tasks.Task<User> UpdateRefreshToken(User user, string? refreshToken)
    {
        user.Refreshtoken = refreshToken;
        _context.Update(user);
        await _context.SaveChangesAsync();
        return user;
    }

    public async System.Threading.Tasks.Task UpdatePasswordAsync(User user, string newHashedPassword)
    {
        user.Password = newHashedPassword;
        // invalidate refresh token on password change
        user.Refreshtoken = null;
        _context.Update(user);
        await _context.SaveChangesAsync();
    }

    public async System.Threading.Tasks.Task<User?> GetByIdAsync(int userId)
    {
        return await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
    }

    public async System.Threading.Tasks.Task<CurrentUserDto?> GetCurrentUserAsync(int userId)
    {
        return await _context.Users
            .AsNoTracking()
            .Where(u => u.UserId == userId)
            .Select(u => new CurrentUserDto
            {
                UserId = u.UserId,
                Username = u.Username,
                Email = u.Email,
                PhoneNumber = u.PhoneNumber,
                Role = u.Role,
                PositionName = u.PositionName ?? (u.Position != null ? u.Position.PositionName : null)
            })
            .FirstOrDefaultAsync();
    }

    public async System.Threading.Tasks.Task<(System.Collections.Generic.List<UserBasicDto> subordinates, System.Collections.Generic.List<UserBasicDto> peers)> GetSubordinatesAndPeersAsync(int callerId)
    {
        var callerParent = await _context.Users
            .AsNoTracking()
            .Where(u => u.UserId == callerId)
            .Select(u => u.UserParent)
            .FirstOrDefaultAsync();

        var subordinates = await _context.Users
            .AsNoTracking()
            .Where(u => u.UserParent == callerId)
            .Select(u => new UserBasicDto
            {
                UserId = u.UserId,
                Username = u.Username,
                FullName = u.FullName,
                Email = u.Email,
                OrgId = u.OrgId,
                UnitId = u.UnitId,
                Role = u.Role,
            })
            .ToListAsync();

        var peers = await _context.Users
            .AsNoTracking()
            .Where(u => u.UserParent == callerParent && u.UserId != callerId)
            .Select(u => new UserBasicDto
            {
                UserId = u.UserId,
                Username = u.Username,
                FullName = u.FullName,
                Email = u.Email,
                OrgId = u.OrgId,
                UnitId = u.UnitId,
                Role = u.Role,
            })
            .ToListAsync();

        return (subordinates, peers);
    }
    
    
}