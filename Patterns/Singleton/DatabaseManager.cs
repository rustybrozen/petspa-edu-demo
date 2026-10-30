using Microsoft.Data.Sqlite;

namespace PetSpaApi.Core;

public class DatabaseManager
{
    private static DatabaseManager? _instance = null;
    private static readonly object _lock = new object();
    private readonly string _connectionString = "Data Source=petspa.db";

    private DatabaseManager()
    {
        InitDatabase();
    }

    public static DatabaseManager GetInstance()
    {
        if (_instance == null)
        {
            lock (_lock)
            {
                if (_instance == null)
                {
                    _instance = new DatabaseManager();
                }
            }
        }
        return _instance;
    }

    public SqliteConnection CreateConnection()
    {
        SqliteConnection connection = new SqliteConnection(_connectionString);
        connection.Open();
        return connection;
    }

    private void InitDatabase()
    {
        using SqliteConnection connection = CreateConnection();
        SqliteCommand command = connection.CreateCommand();

        command.CommandText = @"
            PRAGMA foreign_keys = ON;

            CREATE TABLE IF NOT EXISTS Customers (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Phone TEXT NOT NULL UNIQUE,
                Password TEXT NOT NULL,
                FullName TEXT DEFAULT '',
                CustomerType TEXT DEFAULT 'STANDARD'
            );

            CREATE TABLE IF NOT EXISTS Pets (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                CustomerId INTEGER NOT NULL,
                NameAndBreed TEXT NOT NULL,
                FOREIGN KEY (CustomerId) REFERENCES Customers(Id) ON DELETE CASCADE
            );

            CREATE TABLE IF NOT EXISTS Services (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL,
                BasePrice REAL NOT NULL
            );

            CREATE TABLE IF NOT EXISTS Bookings (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                CustomerId INTEGER NOT NULL,
                PetId INTEGER,
                ServiceId INTEGER NOT NULL,
                BookingDate TEXT NOT NULL,
                Status TEXT DEFAULT 'CONFIRMED',
                DepositAmount REAL DEFAULT 0,
                Snapshot_PetName TEXT NOT NULL,
                Snapshot_ServiceName TEXT NOT NULL,
                FOREIGN KEY (CustomerId) REFERENCES Customers(Id),
                FOREIGN KEY (PetId) REFERENCES Pets(Id) ON DELETE SET NULL,
                FOREIGN KEY (ServiceId) REFERENCES Services(Id)
            );

            CREATE TABLE IF NOT EXISTS Invoices (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                BookingId INTEGER NOT NULL UNIQUE,
                CreatedAt TEXT NOT NULL,
                FinalTotal REAL NOT NULL,
                DepositPaid REAL NOT NULL,
                RemainingAmount REAL NOT NULL,
                FOREIGN KEY (BookingId) REFERENCES Bookings(Id)
            );

            INSERT OR IGNORE INTO Services (Id, Name, BasePrice) VALUES
            (1, 'Combo Tắm sấy & Vệ sinh cơ bản', 150000),
            (2, 'Cắt tỉa & Tạo kiểu lông chuyên sâu', 300000),
            (3, 'Combo Spa thư giãn phục hồi', 400000),
            (4, 'Khách sạn thú cưng tiêu chuẩn', 180000),
            (5, 'Khách sạn thú cưng VIP', 300000);

            INSERT OR IGNORE INTO Customers (Id, Phone, Password, CustomerType, FullName) VALUES
            (1, 'admin', 'admin', 'ADMIN', 'Quản trị viên'),
            (2, '111111', '111111', 'STANDARD', 'Nguyễn Văn Khách');

            UPDATE Customers SET FullName = 'Quản trị viên' WHERE Id = 1 AND (FullName IS NULL OR FullName = '');
            UPDATE Customers SET FullName = 'Nguyễn Văn Khách' WHERE Id = 2 AND (FullName IS NULL OR FullName = '');

            INSERT OR IGNORE INTO Pets (Id, CustomerId, NameAndBreed) VALUES
            (1, 2, 'Mun chó trắng'),
            (2, 2, 'Mèo xám');
        ";

        command.ExecuteNonQuery();

        try
        {
            using var alterCmd = connection.CreateCommand();
            alterCmd.CommandText = "ALTER TABLE Customers ADD COLUMN FullName TEXT DEFAULT '';";
            alterCmd.ExecuteNonQuery();
        }
        catch { }
    }
}
