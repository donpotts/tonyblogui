using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TonyBlogUI.Server.Data;
using TonyBlogUI.Shared;

namespace TonyBlogUI.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class UsersController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    public UsersController(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager)
    {
        _userManager = userManager;
        _roleManager = roleManager;
    }

    /// <summary>
    /// Get the count of admin users in the system
    /// </summary>
    private async Task<int> GetAdminCountAsync()
    {
        var adminUsers = await _userManager.GetUsersInRoleAsync("Admin");
        return adminUsers.Count;
    }

    /// <summary>
    /// Check if the user is currently an admin
    /// </summary>
    private async Task<bool> IsUserAdminAsync(ApplicationUser user)
    {
        return await _userManager.IsInRoleAsync(user, "Admin");
    }

    /// <summary>
    /// Get all users (Admin only)
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<UserDto>>> GetUsers()
    {
        var users = await _userManager.Users.ToListAsync();
        var userDtos = new List<UserDto>();

        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            userDtos.Add(new UserDto
            {
                Id = user.Id,
                Email = user.Email ?? "",
                FirstName = user.FirstName,
                LastName = user.LastName,
                Roles = roles.ToList()
            });
        }

        return Ok(userDtos);
    }

    /// <summary>
    /// Get user by ID (Admin only)
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<UserDto>> GetUser(string id)
    {
        var user = await _userManager.FindByIdAsync(id);

        if (user == null)
        {
            return NotFound(new UserResponse
            {
                Success = false,
                Message = "User not found"
            });
        }

        var roles = await _userManager.GetRolesAsync(user);

        return Ok(new UserDto
        {
            Id = user.Id,
            Email = user.Email ?? "",
            FirstName = user.FirstName,
            LastName = user.LastName,
            Roles = roles.ToList()
        });
    }

    /// <summary>
    /// Get available roles (Admin only)
    /// </summary>
    [HttpGet("roles")]
    public async Task<ActionResult<List<string>>> GetRoles()
    {
        var roles = await _roleManager.Roles.Select(r => r.Name!).ToListAsync();
        return Ok(roles);
    }

    /// <summary>
    /// Create a new user (Admin only)
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<UserResponse>> CreateUser([FromBody] CreateUserRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        // Check if email already exists
        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser != null)
        {
            return BadRequest(new UserResponse
            {
                Success = false,
                Message = "A user with this email already exists"
            });
        }

        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName
        };

        var result = await _userManager.CreateAsync(user, request.Password);

        if (!result.Succeeded)
        {
            return BadRequest(new UserResponse
            {
                Success = false,
                Message = string.Join(", ", result.Errors.Select(e => e.Description))
            });
        }

        // Assign roles
        foreach (var role in request.Roles)
        {
            if (await _roleManager.RoleExistsAsync(role))
            {
                await _userManager.AddToRoleAsync(user, role);
            }
        }

        var roles = await _userManager.GetRolesAsync(user);

        return CreatedAtAction(nameof(GetUser), new { id = user.Id }, new UserResponse
        {
            Success = true,
            Message = "User created successfully",
            User = new UserDto
            {
                Id = user.Id,
                Email = user.Email ?? "",
                FirstName = user.FirstName,
                LastName = user.LastName,
                Roles = roles.ToList()
            }
        });
    }

    /// <summary>
    /// Update a user (Admin only)
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<UserResponse>> UpdateUser(string id, [FromBody] UpdateUserRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var user = await _userManager.FindByIdAsync(id);

        if (user == null)
        {
            return NotFound(new UserResponse
            {
                Success = false,
                Message = "User not found"
            });
        }

        // Check if email is being changed to one that already exists
        if (user.Email != request.Email)
        {
            var existingUser = await _userManager.FindByEmailAsync(request.Email);
            if (existingUser != null)
            {
                return BadRequest(new UserResponse
                {
                    Success = false,
                    Message = "A user with this email already exists"
                });
            }
        }

        // Check if this user is currently an admin
        var isCurrentlyAdmin = await IsUserAdminAsync(user);
        var willBeAdmin = request.Roles.Contains("Admin");

        // If removing admin role, check if this would leave no admins
        if (isCurrentlyAdmin && !willBeAdmin)
        {
            var adminCount = await GetAdminCountAsync();
            if (adminCount <= 1)
            {
                return BadRequest(new UserResponse
                {
                    Success = false,
                    Message = "Cannot remove Admin role. At least one admin must exist in the system."
                });
            }
        }

        user.Email = request.Email;
        user.UserName = request.Email;
        user.FirstName = request.FirstName;
        user.LastName = request.LastName;

        var result = await _userManager.UpdateAsync(user);

        if (!result.Succeeded)
        {
            return BadRequest(new UserResponse
            {
                Success = false,
                Message = string.Join(", ", result.Errors.Select(e => e.Description))
            });
        }

        // Update password if provided
        if (!string.IsNullOrEmpty(request.NewPassword))
        {
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var passwordResult = await _userManager.ResetPasswordAsync(user, token, request.NewPassword);

            if (!passwordResult.Succeeded)
            {
                return BadRequest(new UserResponse
                {
                    Success = false,
                    Message = string.Join(", ", passwordResult.Errors.Select(e => e.Description))
                });
            }
        }

        // Update roles
        var currentRoles = await _userManager.GetRolesAsync(user);
        await _userManager.RemoveFromRolesAsync(user, currentRoles);

        foreach (var role in request.Roles)
        {
            if (await _roleManager.RoleExistsAsync(role))
            {
                await _userManager.AddToRoleAsync(user, role);
            }
        }

        var updatedRoles = await _userManager.GetRolesAsync(user);

        return Ok(new UserResponse
        {
            Success = true,
            Message = "User updated successfully",
            User = new UserDto
            {
                Id = user.Id,
                Email = user.Email ?? "",
                FirstName = user.FirstName,
                LastName = user.LastName,
                Roles = updatedRoles.ToList()
            }
        });
    }

    /// <summary>
    /// Delete a user (Admin only)
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult<UserResponse>> DeleteUser(string id)
    {
        var user = await _userManager.FindByIdAsync(id);

        if (user == null)
        {
            return NotFound(new UserResponse
            {
                Success = false,
                Message = "User not found"
            });
        }

        // Prevent admin from deleting themselves
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (user.Id == currentUserId)
        {
            return BadRequest(new UserResponse
            {
                Success = false,
                Message = "You cannot delete your own account"
            });
        }

        // Check if this user is an admin and if they're the last one
        var isAdmin = await IsUserAdminAsync(user);
        if (isAdmin)
        {
            var adminCount = await GetAdminCountAsync();
            if (adminCount <= 1)
            {
                return BadRequest(new UserResponse
                {
                    Success = false,
                    Message = "Cannot delete the last admin. At least one admin must exist in the system."
                });
            }
        }

        var result = await _userManager.DeleteAsync(user);

        if (!result.Succeeded)
        {
            return BadRequest(new UserResponse
            {
                Success = false,
                Message = string.Join(", ", result.Errors.Select(e => e.Description))
            });
        }

        return Ok(new UserResponse
        {
            Success = true,
            Message = "User deleted successfully"
        });
    }
}
