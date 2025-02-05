using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using UserManagementAPI.Models;
using UserManagementAPI.DTOs;
using System.Threading.Tasks;
using UserManagement.Data;
using UserManagement.DTOs;

namespace UserManagementAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IPasswordHasher<User> _passwordHasher;

        public UserController(AppDbContext context, IPasswordHasher<User> passwordHasher)
        {
            _context = context;
            _passwordHasher = passwordHasher;
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserDTO createUserDTO)
        {
            if (createUserDTO == null)
            {
                return BadRequest("Thông tin tài khoản không hợp lệ");
            }

            if (await _context.Users.AnyAsync(u => u.Email == createUserDTO.Email || u.Username == createUserDTO.Username))
            {
                return Conflict("Email hoặc Username đã tồn tại");
            }

            var user = new User
            {
                Email = createUserDTO.Email,
                Username = createUserDTO.Username
            };

            user.Password = _passwordHasher.HashPassword(user, createUserDTO.Password);

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var response = new
            {
                Id = user.Id,
                Email = user.Email,
                Username = user.Username
            };

            return CreatedAtAction(nameof(CreateUser), new { id = user.Id }, response);
        }


        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto changePasswordDto)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == changePasswordDto.Email);
            if (user == null)
            {
                return NotFound("Người dùng không tồn tại.");
            }

            try
            {
                var passwordVerificationResult = _passwordHasher.VerifyHashedPassword(user, user.Password, changePasswordDto.Password);
                if (passwordVerificationResult != PasswordVerificationResult.Success)
                {
                    return BadRequest("Mật khẩu cũ không chính xác.");
                }
            }
            catch (FormatException)
            {
                return BadRequest("Mật khẩu trong cơ sở dữ liệu không hợp lệ.");
            }

            user.Password = _passwordHasher.HashPassword(user, changePasswordDto.NewPassword);
            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            return Ok("Đổi mật khẩu thành công.");
        }



        [HttpPost("update-user")]
        public async Task<IActionResult> UpdateUser([FromBody] UpdateUserDto updateUserDto)
        {
            if (updateUserDto == null)
            {
                return BadRequest(new UpdateUserResponseDto
                {
                    Success = false,
                    Message = "Thông tin cập nhật không hợp lệ"
                });
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == updateUserDto.CurrentEmail && u.Username == updateUserDto.CurrentUsername);

            if (user == null)
            {
                return NotFound(new UpdateUserResponseDto
                {
                    Success = false,
                    Message = "Không tìm thấy người dùng với thông tin hiện tại"
                });
            }

            if (await _context.Users.AnyAsync(u => u.Email == updateUserDto.NewEmail && u.Id != user.Id))
            {
                return Conflict(new UpdateUserResponseDto
                {
                    Success = false,
                    Message = "Email mới đã tồn tại"
                });
            }

            if (await _context.Users.AnyAsync(u => u.Username == updateUserDto.NewUsername && u.Id != user.Id))
            {
                return Conflict(new UpdateUserResponseDto
                {
                    Success = false,
                    Message = "Username mới đã tồn tại"
                });
            }

            user.Email = updateUserDto.NewEmail;
            user.Username = updateUserDto.NewUsername;

            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            return Ok(new UpdateUserResponseDto
            {
                Success = true,
                Message = "Cập nhật thông tin cá nhân thành công"
            });
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequestDto request)
        {
            if (request == null || string.IsNullOrEmpty(request.Email))
            {
                return BadRequest(new ForgotPasswordResponseDto
                {
                    Message = "Email không hợp lệ"
                });
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == request.Email);

            if (user == null)
            {
                return NotFound(new ForgotPasswordResponseDto
                {
                    Message = "Không tìm thấy người dùng với email này"
                });
            }

            var newPassword = GenerateRandomPassword();

            user.Password = newPassword;

            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            return Ok(new ForgotPasswordResponseDto
            {
                Message = "Mật khẩu mới đã được tạo và gửi qua email",
                NewPassword = newPassword
            });
        }
        private string GenerateRandomPassword(int length = 10)
        {
            const string validChars = "ABCDEFGHJKLMNOPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz0123456789";
            var random = new Random();
            return new string(Enumerable.Repeat(validChars, length)
                                        .Select(s => s[random.Next(s.Length)])
                                        .ToArray());
        }
        [HttpPost("hash-all-passwords")]
        public async Task<IActionResult> HashAllPasswords()
        {
            var users = await _context.Users.ToListAsync();
            foreach (var user in users)
            {
                try
                {
                    var result = _passwordHasher.VerifyHashedPassword(user, user.Password, "dummyPassword");
                    continue;
                }
                catch (FormatException)
                {
                    user.Password = _passwordHasher.HashPassword(user, user.Password);
                }
            }

            await _context.SaveChangesAsync();
            return Ok("Cập nhật mật khẩu thành công.");
        }


    }
}
