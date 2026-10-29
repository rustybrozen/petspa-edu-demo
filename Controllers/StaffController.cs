using Microsoft.AspNetCore.Mvc;
using PetSpaApi.Core;
using PetSpaApi.Services.ExportBridge;
using PetSpaApi.Services.PricingStrategy;

namespace PetSpaApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StaffController : ControllerBase
{
    public class UpdateStatusReq
    {
        public string Status { get; set; } = "";
    }

    public class CalculatePriceReq
    {
        public int BookingId { get; set; }
        public string Strategy { get; set; } = "WEEKDAY";
    }

    public class CheckoutReq
    {
        public int BookingId { get; set; }
    }

    [HttpGet("bookings")]
    public IActionResult GetAllBookings()
    {
        var list = new List<object>();
        using var conn = DatabaseManager.GetInstance().CreateConnection();
        var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT b.Id, c.Phone, b.Snapshot_PetName, b.Snapshot_ServiceName,
                   b.BookingDate, s.BasePrice, b.DepositAmount, b.Status
            FROM Bookings b
            JOIN Customers c ON b.CustomerId = c.Id
            JOIN Services s ON b.ServiceId = s.Id
            ORDER BY b.Id DESC";

        using var r = cmd.ExecuteReader();
        while (r.Read())
        {
            list.Add(new
            {
                id = r.GetInt32(0),
                customer = r.GetString(1),
                phone = r.GetString(1),
                pet = r.GetString(2),
                sName = r.GetString(3),
                date = r.GetString(4),
                basePrice = r.GetDouble(5),
                depositPaid = r.GetDouble(6),
                status = r.GetString(7)
            });
        }
        return Ok(list);
    }

    [HttpPut("bookings/{id}/status")]
    public IActionResult UpdateBookingStatus(int id, [FromBody] UpdateStatusReq req)
    {
        using var conn = DatabaseManager.GetInstance().CreateConnection();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "UPDATE Bookings SET Status = $status WHERE Id = $id";
        cmd.Parameters.AddWithValue("$status", req.Status.ToUpper());
        cmd.Parameters.AddWithValue("$id", id);

        int rows = cmd.ExecuteNonQuery();
        if (rows == 0)
        {
            return NotFound(new { message = "Không tìm thấy lịch hẹn!" });
        }

        return Ok(new { message = "Cập nhật trạng thái thành công!" });
    }

    [HttpPost("billing/calculate")]
    public IActionResult CalculatePrice([FromBody] CalculatePriceReq req)
    {
        using var conn = DatabaseManager.GetInstance().CreateConnection();
        var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT s.BasePrice, b.DepositAmount
            FROM Bookings b
            JOIN Services s ON b.ServiceId = s.Id
            WHERE b.Id = $id";
        cmd.Parameters.AddWithValue("$id", req.BookingId);

        using var reader = cmd.ExecuteReader();
        if (!reader.Read())
        {
            return NotFound(new { message = "Không tìm thấy đơn!" });
        }

        double basePrice = reader.GetDouble(0);
        double depositPaid = reader.GetDouble(1);
        reader.Close();

        IPricingStrategy strategy;
        if (req.Strategy.ToUpper() == "WEEKEND")
        {
            strategy = new WeekendPricingStrategy();
        }
        else
        {
            strategy = new WeekdayPricingStrategy();
        }

        PricingContext context = new PricingContext(strategy);
        double grossTotal = context.ExecuteStrategy(basePrice);
        double netPayable = grossTotal - depositPaid;

        return Ok(new
        {
            bookingId = req.BookingId,
            basePrice = basePrice,
            grossTotal = grossTotal,
            depositPaid = depositPaid,
            netPayable = netPayable,
            policyName = context.GetPolicyName(),
            policyCode = context.GetPolicyCode()
        });
    }

    [HttpPost("billing/checkout")]
    public IActionResult Checkout([FromBody] CheckoutReq req)
    {
        using var conn = DatabaseManager.GetInstance().CreateConnection();

        var queryCmd = conn.CreateCommand();
        queryCmd.CommandText = @"
            SELECT s.BasePrice, b.DepositAmount, b.BookingDate
            FROM Bookings b
            JOIN Services s ON b.ServiceId = s.Id
            WHERE b.Id = $id";
        queryCmd.Parameters.AddWithValue("$id", req.BookingId);

        using var reader = queryCmd.ExecuteReader();
        if (!reader.Read())
        {
            return NotFound(new { message = "Không tìm thấy thông tin lịch hẹn để thanh toán!" });
        }

        double basePrice = reader.GetDouble(0);
        double depositPaid = reader.GetDouble(1);
        string bookingDate = reader.GetString(2);
        reader.Close();

        DateTime bDate = DateTime.TryParse(bookingDate, out var dt) ? dt : DateTime.Now;
        bool isWeekend = (bDate.DayOfWeek == DayOfWeek.Saturday || bDate.DayOfWeek == DayOfWeek.Sunday);

        IPricingStrategy strategy = isWeekend ? new WeekendPricingStrategy() : new WeekdayPricingStrategy();
        PricingContext context = new PricingContext(strategy);
        double finalTotal = context.ExecuteStrategy(basePrice);
        double remainingAmount = Math.Max(0, finalTotal - depositPaid);

        var updateCmd = conn.CreateCommand();
        updateCmd.CommandText = "UPDATE Bookings SET Status = 'COMPLETED' WHERE Id = $id";
        updateCmd.Parameters.AddWithValue("$id", req.BookingId);
        updateCmd.ExecuteNonQuery();

        var invoiceCmd = conn.CreateCommand();
        invoiceCmd.CommandText = @"
            INSERT OR REPLACE INTO Invoices (BookingId, CreatedAt, FinalTotal, DepositPaid, RemainingAmount)
            VALUES ($bid, $created, $final, $dep, $rem)";
        invoiceCmd.Parameters.AddWithValue("$bid", req.BookingId);
        invoiceCmd.Parameters.AddWithValue("$created", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        invoiceCmd.Parameters.AddWithValue("$final", finalTotal);
        invoiceCmd.Parameters.AddWithValue("$dep", depositPaid);
        invoiceCmd.Parameters.AddWithValue("$rem", remainingAmount);
        invoiceCmd.ExecuteNonQuery();

        return Ok(new
        {
            message = "Thanh toán hoàn tất và đã tạo hóa đơn!",
            bookingId = req.BookingId,
            finalTotal = finalTotal,
            depositPaid = depositPaid,
            remainingAmount = remainingAmount
        });
    }

    [HttpGet("bookings/{id}/export-invoice")]
    public IActionResult ExportInvoice(int id, [FromQuery] string format = "online", [FromQuery] string strategy = "WEEKDAY")
    {
        using var conn = DatabaseManager.GetInstance().CreateConnection();
        var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT c.Phone, b.Snapshot_PetName, b.Snapshot_ServiceName, s.BasePrice, b.DepositAmount
            FROM Bookings b
            JOIN Customers c ON b.CustomerId = c.Id
            JOIN Services s ON b.ServiceId = s.Id
            WHERE b.Id = $id";
        cmd.Parameters.AddWithValue("$id", id);

        using var r = cmd.ExecuteReader();
        if (!r.Read())
        {
            return NotFound(new { message = "Không tìm thấy dữ liệu!" });
        }

        string custName = r.GetString(0);
        string petName = r.GetString(1);
        string serviceName = r.GetString(2);
        double basePrice = r.GetDouble(3);
        double deposit = r.GetDouble(4);
        r.Close();

        IPricingStrategy pricingStrategy;
        if (strategy.ToUpper() == "WEEKEND")
        {
            pricingStrategy = new WeekendPricingStrategy();
        }
        else
        {
            pricingStrategy = new WeekdayPricingStrategy();
        }

        PricingContext context = new PricingContext(pricingStrategy);
        double finalAmount = context.ExecuteStrategy(basePrice) - deposit;
        string policyName = context.GetPolicyName();

        IExportFormatter formatter;
        if (format.ToLower() == "offline")
        {
            formatter = new OfflinePosFormatter();
        }
        else
        {
            formatter = new OnlineHtmlFormatter();
        }

        ServiceTicket ticket = new InvoiceTicket(
            formatter,
            id,
            custName,
            petName,
            serviceName,
            finalAmount,
            policyName
        );

        return Ok(new { content = ticket.Export() });
    }

    [HttpGet("services")]
    public IActionResult GetServices()
    {
        var services = new List<object>();
        using var conn = DatabaseManager.GetInstance().CreateConnection();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT Id, Name, BasePrice FROM Services";
        using var r = cmd.ExecuteReader();
        while (r.Read())
        {
            services.Add(new
            {
                id = r.GetInt32(0),
                name = r.GetString(1),
                price = r.GetDouble(2)
            });
        }
        return Ok(services);
    }
}