using Microsoft.AspNetCore.Mvc;
using PetSpaApi.Core;

namespace PetSpaApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    public class AuthRequest
    {
        public string Phone { get; set; } = "";
        public string Password { get; set; } = "";
    }

    [HttpPost("register")]
    public IActionResult Register([FromBody] AuthRequest req)
    {
        using var conn = DatabaseManager.GetInstance().CreateConnection();

        var checkCmd = conn.CreateCommand();
        checkCmd.CommandText = "SELECT COUNT(*) FROM Customers WHERE Phone = $phone";
        checkCmd.Parameters.AddWithValue("$phone", req.Phone.Trim());
        long exists = (long)checkCmd.ExecuteScalar()!;
        if (exists > 0)
        {
            return BadRequest(new { message = "Số điện thoại này đã tồn tại!" });
        }

        var insertCmd = conn.CreateCommand();
        insertCmd.CommandText = "INSERT INTO Customers (Phone, Password, CustomerType) VALUES ($phone, $password, 'STANDARD')";
        insertCmd.Parameters.AddWithValue("$phone", req.Phone.Trim());
        insertCmd.Parameters.AddWithValue("$password", req.Password);
        insertCmd.ExecuteNonQuery();

        return Ok(new { message = "Đăng ký thành công!" });
    }

    [HttpPost("login")]
    public IActionResult Login([FromBody] AuthRequest req)
    {
        using var conn = DatabaseManager.GetInstance().CreateConnection();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT Id, Phone, CustomerType, COALESCE(FullName, '') FROM Customers WHERE Phone = $phone AND Password = $password";
        cmd.Parameters.AddWithValue("$phone", req.Phone.Trim());
        cmd.Parameters.AddWithValue("$password", req.Password);

        using var reader = cmd.ExecuteReader();
        if (reader.Read())
        {
            var user = new
            {
                id = reader.GetInt32(0),
                phone = reader.GetString(1),
                customerType = reader.GetString(2),
                fullName = reader.GetString(3)
            };
            return Ok(new { message = "Đăng nhập thành công!", user = user });
        }

        return Unauthorized(new { message = "Sai số điện thoại hoặc mật khẩu!" });
    }
}




