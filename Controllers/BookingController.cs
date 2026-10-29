using Microsoft.AspNetCore.Mvc;
using PetSpaApi.Core;
using PetSpaApi.Services.ExportBridge;
using PetSpaApi.Services.PricingStrategy;

namespace PetSpaApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BookingController : ControllerBase
{
    public class CreateBookingReq
    {
        public int CustomerId { get; set; }
        public int PetId { get; set; }
        public int ServiceId { get; set; }
        public string BookingDate { get; set; } = "";
    }

    [HttpPost]
    public IActionResult CreateBooking([FromBody] CreateBookingReq req)
    {
        using var conn = DatabaseManager.GetInstance().CreateConnection();

        var infoCmd = conn.CreateCommand();
        infoCmd.CommandText = @"
            SELECT (SELECT NameAndBreed FROM Pets WHERE Id = $pid), Name, BasePrice
            FROM Services WHERE Id = $sid";
        infoCmd.Parameters.AddWithValue("$pid", req.PetId);
        infoCmd.Parameters.AddWithValue("$sid", req.ServiceId);

        using var r = infoCmd.ExecuteReader();
        if (!r.Read() || r.IsDBNull(0))
        {
            return BadRequest(new { message = "Thú cưng hoặc dịch vụ không hợp lệ!" });
        }

        string petName = r.GetString(0);
        string serviceName = r.GetString(1);
        double basePrice = r.GetDouble(2);
        r.Close();

        DateTime bDate = DateTime.Parse(req.BookingDate);
        bool isWeekend = (bDate.DayOfWeek == DayOfWeek.Saturday || bDate.DayOfWeek == DayOfWeek.Sunday);

        IPricingStrategy strategy;
        if (isWeekend)
        {
            strategy = new WeekendPricingStrategy();
        }
        else
        {
            strategy = new WeekdayPricingStrategy();
        }

        PricingContext context = new PricingContext(strategy);
        double estimatedTotal = context.ExecuteStrategy(basePrice);
        double depositAmount = Math.Round(estimatedTotal * 0.3);

        var insCmd = conn.CreateCommand();
        insCmd.CommandText = @"
            INSERT INTO Bookings (
                CustomerId, PetId, ServiceId, BookingDate, Status,
                DepositAmount, Snapshot_PetName, Snapshot_ServiceName
            ) VALUES (
                $cid, $pid, $sid, $date, 'CONFIRMED',
                $deposit, $pName, $sName
            );
            SELECT last_insert_rowid();";
        insCmd.Parameters.AddWithValue("$cid", req.CustomerId);
        insCmd.Parameters.AddWithValue("$pid", req.PetId);
        insCmd.Parameters.AddWithValue("$sid", req.ServiceId);
        insCmd.Parameters.AddWithValue("$date", req.BookingDate);
        insCmd.Parameters.AddWithValue("$deposit", depositAmount);
        insCmd.Parameters.AddWithValue("$pName", petName);
        insCmd.Parameters.AddWithValue("$sName", serviceName);

        long newBookingId = (long)insCmd.ExecuteScalar()!;

        return Ok(new
        {
            message = "Đặt lịch và tính cọc thành công!",
            bookingId = newBookingId,
            depositAmount = depositAmount,
            estimatedTotal = estimatedTotal,
            policy = context.GetPolicyName()
        });
    }

    [HttpGet("customer/{customerId}")]
    public IActionResult GetCustomerBookings(int customerId)
    {
        var list = new List<object>();
        using var conn = DatabaseManager.GetInstance().CreateConnection();
        var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT Id, Snapshot_PetName, Snapshot_ServiceName, BookingDate, Status, DepositAmount
            FROM Bookings WHERE CustomerId = $cid ORDER BY Id DESC";
        cmd.Parameters.AddWithValue("$cid", customerId);

        using var r = cmd.ExecuteReader();
        while (r.Read())
        {
            list.Add(new
            {
                id = r.GetInt32(0),
                petName = r.GetString(1),
                sName = r.GetString(2),
                date = r.GetString(3),
                status = r.GetString(4),
                depositAmount = r.GetDouble(5)
            });
        }
        return Ok(list);
    }

    [HttpGet("{id}/export")]
    public IActionResult ExportTicket(int id, [FromQuery] string format = "online")
    {
        using var conn = DatabaseManager.GetInstance().CreateConnection();
        var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT c.Phone, b.Snapshot_PetName, b.Snapshot_ServiceName, b.BookingDate, b.Status
            FROM Bookings b
            JOIN Customers c ON b.CustomerId = c.Id
            WHERE b.Id = $id";
        cmd.Parameters.AddWithValue("$id", id);

        using var r = cmd.ExecuteReader();
        if (!r.Read())
        {
            return NotFound(new { message = "Không tìm thấy phiếu hẹn" });
        }

        IExportFormatter formatter;
        if (format.ToLower() == "offline")
        {
            formatter = new OfflinePosFormatter();
        }
        else
        {
            formatter = new OnlineHtmlFormatter();
        }

        ServiceTicket ticket = new AppointmentTicket(
            formatter,
            id,
            r.GetString(0),
            r.GetString(1),
            r.GetString(2),
            r.GetString(3),
            r.GetString(4)
        );

        return Ok(new { content = ticket.Export() });
    }
}
