namespace UserManagement.DTOs
{
    public class ChangePasswordDto
    {
        public string Email { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string NewPassword { get; set; }
    }
}
