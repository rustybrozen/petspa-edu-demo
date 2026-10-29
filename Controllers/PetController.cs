using Microsoft.AspNetCore.Mvc;
using PetSpaApi.Core;

namespace PetSpaApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PetController : ControllerBase
{
    public class CreatePetReq
    {
        public int CustomerId { get; set; }
        public string NameAndBreed { get; set; } = "";
    }

    [HttpGet("customer/{customerId}")]
    public IActionResult GetByCustomer(int customerId)
    {
        var pets = new List<object>();
        using var conn = DatabaseManager.GetInstance().CreateConnection();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT Id, NameAndBreed FROM Pets WHERE CustomerId = $cid ORDER BY Id DESC";
        cmd.Parameters.AddWithValue("$cid", customerId);

        using var r = cmd.ExecuteReader();
        while (r.Read())
        {
            pets.Add(new
            {
                id = r.GetInt32(0),
                nameAndBreed = r.GetString(1)
            });
        }
        return Ok(pets);
    }

    [HttpPost]
    public IActionResult Create([FromBody] CreatePetReq req)
    {
        using var conn = DatabaseManager.GetInstance().CreateConnection();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "INSERT INTO Pets (CustomerId, NameAndBreed) VALUES ($cid, $info)";
        cmd.Parameters.AddWithValue("$cid", req.CustomerId);
        cmd.Parameters.AddWithValue("$info", req.NameAndBreed);
        cmd.ExecuteNonQuery();

        return Ok(new { message = "Thêm thú cưng thành công!" });
    }

    [HttpDelete("{id}")]
    public IActionResult Delete(int id)
    {
        using var conn = DatabaseManager.GetInstance().CreateConnection();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM Pets WHERE Id = $id";
        cmd.Parameters.AddWithValue("$id", id);
        cmd.ExecuteNonQuery();

        return Ok(new { message = "Xóa thành công!" });
    }
}