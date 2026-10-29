using Microsoft.AspNetCore.Mvc;
using PetSpaApi.Core;

namespace PetSpaApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProfileController : ControllerBase
{
    public class UpdateProfileReq
    {
        public string FullName { get; set; } = "";
        public string Phone { get; set; } = "";
        public string? Password { get; set; }
    }

    [HttpGet("{id}")]
    public IActionResult GetProfile(int id)
    {
        using var conn = DatabaseManager.GetInstance().CreateConnection();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT Id, Phone, COALESCE(FullName, ''), CustomerType FROM Customers WHERE Id = $id";
        cmd.Parameters.AddWithValue("$id", id);

        using var reader = cmd.ExecuteReader();
        if (reader.Read())
        {
            return Ok(new
            {
                id = reader.GetInt32(0),
                phone = reader.GetString(1),
                fullName = reader.GetString(2),
                customerType = reader.GetString(3)
            });
        }

        return NotFound(new { message = "Không tìm thấy thông tin tài khoản!" });
    }

    [HttpPut("{id}")]
    public IActionResult UpdateProfile(int id, [FromBody] UpdateProfileReq req)
    {
        string phone = req.Phone?.Trim() ?? "";
        string fullName = req.FullName?.Trim() ?? "";

        if (string.IsNullOrEmpty(phone))
        {
            return BadRequest(new { message = "Số điện thoại không được để trống!" });
        }

        using var conn = DatabaseManager.GetInstance().CreateConnection();

        var checkCmd = conn.CreateCommand();
        checkCmd.CommandText = "SELECT COUNT(*) FROM Customers WHERE Phone = $phone AND Id != $id";
        checkCmd.Parameters.AddWithValue("$phone", phone);
        checkCmd.Parameters.AddWithValue("$id", id);
        long exists = (long)checkCmd.ExecuteScalar()!;
        if (exists > 0)
        {
            return BadRequest(new { message = "Số điện thoại này đã được sử dụng bởi tài khoản khác!" });
        }

        var updateCmd = conn.CreateCommand();
        if (!string.IsNullOrWhiteSpace(req.Password))
        {
            updateCmd.CommandText = "UPDATE Customers SET FullName = $name, Phone = $phone, Password = $pwd WHERE Id = $id";
            updateCmd.Parameters.AddWithValue("$name", fullName);
            updateCmd.Parameters.AddWithValue("$phone", phone);
            updateCmd.Parameters.AddWithValue("$pwd", req.Password.Trim());
            updateCmd.Parameters.AddWithValue("$id", id);
        }
        else
        {
            updateCmd.CommandText = "UPDATE Customers SET FullName = $name, Phone = $phone WHERE Id = $id";
            updateCmd.Parameters.AddWithValue("$name", fullName);
            updateCmd.Parameters.AddWithValue("$phone", phone);
            updateCmd.Parameters.AddWithValue("$id", id);
        }

        int affected = updateCmd.ExecuteNonQuery();
        if (affected == 0)
        {
            return NotFound(new { message = "Không tìm thấy tài khoản để cập nhật!" });
        }

        var fetchCmd = conn.CreateCommand();
        fetchCmd.CommandText = "SELECT Id, Phone, COALESCE(FullName, ''), CustomerType FROM Customers WHERE Id = $id";
        fetchCmd.Parameters.AddWithValue("$id", id);
        using var reader = fetchCmd.ExecuteReader();
        if (reader.Read())
        {
            var updatedUser = new
            {
                id = reader.GetInt32(0),
                phone = reader.GetString(1),
                fullName = reader.GetString(2),
                customerType = reader.GetString(3)
            };
            return Ok(new { message = "Cập nhật thông tin thành công!", user = updatedUser });
        }

        return Ok(new { message = "Cập nhật thành công!" });
    }
}
