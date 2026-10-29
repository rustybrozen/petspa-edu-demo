namespace PetSpaApi.Services.ExportBridge;

public interface IExportFormatter
{
    string FormatHeader(string title, string subTitle);
    string FormatInfoRow(string label, string value);
    string FormatDivider();
    string FormatTotal(string label, double amount);
}









public class OnlineHtmlFormatter : IExportFormatter
{
    public string FormatHeader(string title, string subTitle)
    {
        return "<div class='text-center mb-3'>"
             + $"<h6 class='fw-bold mb-0 text-success'>{title}</h6>"
             + $"<span class='badge bg-light text-secondary border mt-1'>{subTitle}</span>"
             + "</div>";
    }

    public string FormatInfoRow(string label, string value)
    {
        return $"<div class='small mb-1'><strong>{label}:</strong> {value}</div>";
    }

    public string FormatDivider()
    {
        return "<hr class='my-2'>";
    }

    public string FormatTotal(string label, double amount)
    {
        return $"<div class='d-flex justify-content-between fw-bold text-primary mt-2'><span>{label}:</span><span>{amount:N0} VNĐ</span></div>";
    }
}

public class OfflinePosFormatter : IExportFormatter
{
    public string FormatHeader(string title, string subTitle)
    {
        return "==============================\n"
             + $"   {title.ToUpper()}\n"
             + $"   {subTitle}\n"
             + "==============================\n";
    }

    public string FormatInfoRow(string label, string value)
    {
        return $"{label}: {value}\n";
    }

    public string FormatDivider()
    {
        return "------------------------------\n";
    }

    public string FormatTotal(string label, double amount)
    {
        return $"{label}: {amount:N0} VND\n";
    }
}




public abstract class ServiceTicket
{
    protected readonly IExportFormatter _formatter;

    protected ServiceTicket(IExportFormatter formatter)
    {
        _formatter = formatter;
    }

    public abstract string Export();
}









public class AppointmentTicket : ServiceTicket
{
    private readonly int _bookingId;
    private readonly string _customerName;
    private readonly string _petName;
    private readonly string _serviceName;
    private readonly string _date;
    private readonly string _statusText;

    public AppointmentTicket(
        IExportFormatter formatter,
        int id,
        string custName,
        string petName,
        string serviceName,
        string date,
        string statusText
    ) : base(formatter)
    {
        _bookingId = id;
        _customerName = custName;
        _petName = petName;
        _serviceName = serviceName;
        _date = date;
        _statusText = statusText;
    }

    public override string Export()
    {
        return _formatter.FormatHeader($"PHIẾU TIẾP NHẬN #{_bookingId}", "Pet Spa")
             + _formatter.FormatInfoRow("Khách hàng", _customerName)
             + _formatter.FormatInfoRow("Thú cưng", _petName)
             + _formatter.FormatInfoRow("Gói dịch vụ", _serviceName)
             + _formatter.FormatInfoRow("Ngày hẹn", _date)
             
             + _formatter.FormatDivider();
    }
}






public class InvoiceTicket : ServiceTicket
{
    private readonly int _bookingId;
    private readonly string _customerName;
    private readonly string _petName;
    private readonly string _serviceName;
    private readonly double _finalAmount;
    private readonly string _policy;

    public InvoiceTicket(
        IExportFormatter formatter,
        int id,
        string custName,
        string petName,
        string serviceName,
        double amount,
        string policy
    ) : base(formatter)
    {
        _bookingId = id;
        _customerName = custName;
        _petName = petName;
        _serviceName = serviceName;
        _finalAmount = amount;
        _policy = policy;
    }

    public override string Export()
    {
        return _formatter.FormatHeader($"HÓA ĐƠN DỊCH VỤ #{_bookingId}", "Pet Spa")
             + _formatter.FormatInfoRow("Khách hàng", _customerName)
             + _formatter.FormatInfoRow("Thú cưng", _petName)
             + _formatter.FormatInfoRow("Dịch vụ", _serviceName)
             + _formatter.FormatInfoRow("Chính sách giá", _policy)
             + _formatter.FormatDivider()
             + _formatter.FormatTotal("Tổng thanh toán", _finalAmount);
    }
}
