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
(2, 'Cắt tỉa & Tạo kiểu lông chuyên sâu', 280000),
(3, 'Combo Spa thư giãn phục hồi', 400000),
(4, 'Khách sạn thú cưng tiêu chuẩn', 180000),
(5, 'Khách sạn thú cưng VIP', 300000);

INSERT OR IGNORE INTO Customers (Id, Phone, Password, CustomerType, FullName) VALUES
(1, 'admin', 'admin', 'ADMIN', 'Quản trị viên'),
(2, '111111', '111111', 'STANDARD', 'Nguyễn Văn Khách');

INSERT OR IGNORE INTO Pets (Id, CustomerId, NameAndBreed) VALUES
(1, 2, 'Milo - Chó Poodle'),
(2, 2, 'Bông - Mèo Anh lông ngắn');