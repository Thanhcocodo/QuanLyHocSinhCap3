CREATE DATABASE QuanLyHocSinh;
GO
USE QuanLyHocSinh;
GO

CREATE TABLE Lop (
    LopId INT IDENTITY(1,1) PRIMARY KEY,
    TenLop NVARCHAR(50) NOT NULL,
    Khoi TINYINT NOT NULL,
    NamHoc NVARCHAR(20) NOT NULL,
    IsActive BIT NOT NULL CONSTRAINT DF_Lop_IsActive DEFAULT 1,
    CONSTRAINT CK_Lop_Khoi CHECK (Khoi IN (10,11,12)),
    CONSTRAINT UQ_Lop UNIQUE (TenLop, Khoi, NamHoc)
);

CREATE TABLE HocSinh (
    HocSinhId INT IDENTITY(1,1) PRIMARY KEY,
    MaHocSinh NVARCHAR(20) NOT NULL,
    HoTen NVARCHAR(100) NOT NULL,
    NgaySinh DATE NOT NULL,
    GioiTinh NVARCHAR(10) NOT NULL,
    DiaChi NVARCHAR(200) NULL,
    SoDienThoai NVARCHAR(15) NULL,
    Email NVARCHAR(100) NULL,
    LopId INT NOT NULL,
    NgayNhapHoc DATE NOT NULL,
    HoTenCha NVARCHAR(100) NULL,
    HoTenMe NVARCHAR(100) NULL,
    SDTChaMe NVARCHAR(15) NULL,
    TrangThai NVARCHAR(50) NOT NULL CONSTRAINT DF_HocSinh_TrangThai DEFAULT N'Đang học',
    NgayTotNghiep DATE NULL,
    CONSTRAINT UQ_HocSinh_MaHocSinh UNIQUE (MaHocSinh),
    CONSTRAINT FK_HocSinh_Lop FOREIGN KEY (LopId) REFERENCES Lop(LopId)
);

CREATE TABLE GiaoVien (
    GiaoVienId INT IDENTITY(1,1) PRIMARY KEY,
    MaGiaoVien NVARCHAR(20) NOT NULL,
    HoTen NVARCHAR(100) NOT NULL,
    NgaySinh DATE NULL,
    GioiTinh NVARCHAR(10) NULL,
    DiaChi NVARCHAR(200) NULL,
    SoDienThoai NVARCHAR(15) NULL,
    Email NVARCHAR(100) NULL,
    ChuyenMon NVARCHAR(50) NULL,
    TrinhDo NVARCHAR(50) NULL,
    NgayVaoTruong DATE NULL,
    TrangThai NVARCHAR(50) NOT NULL CONSTRAINT DF_GiaoVien_TrangThai DEFAULT N'Đang giảng dạy',
    CONSTRAINT UQ_GiaoVien_MaGiaoVien UNIQUE (MaGiaoVien)
);

CREATE TABLE MonHoc (
    MonHocId INT IDENTITY(1,1) PRIMARY KEY,
    MaMH NVARCHAR(20) NOT NULL,
    TenMH NVARCHAR(100) NOT NULL,
    HeSo TINYINT NOT NULL CONSTRAINT DF_MonHoc_HeSo DEFAULT 1,
    CONSTRAINT UQ_MonHoc_MaMH UNIQUE (MaMH)
);

CREATE TABLE PhanCong (
    PhanCongId INT IDENTITY(1,1) PRIMARY KEY,
    LopId INT NOT NULL,
    MonHocId INT NOT NULL,
    GiaoVienId INT NOT NULL,
    NamHoc NVARCHAR(20) NOT NULL,
    HocKy TINYINT NOT NULL,
    IsChuNhiem BIT NOT NULL CONSTRAINT DF_PhanCong_IsChuNhiem DEFAULT 0,
    CONSTRAINT CK_PhanCong_HocKy CHECK (HocKy IN (1,2)),
    CONSTRAINT FK_PhanCong_Lop FOREIGN KEY (LopId) REFERENCES Lop(LopId),
    CONSTRAINT FK_PhanCong_MonHoc FOREIGN KEY (MonHocId) REFERENCES MonHoc(MonHocId),
    CONSTRAINT FK_PhanCong_GiaoVien FOREIGN KEY (GiaoVienId) REFERENCES GiaoVien(GiaoVienId),
    CONSTRAINT UQ_PhanCong UNIQUE (LopId, MonHocId, NamHoc, HocKy)
);

CREATE TABLE Diem (
    DiemId INT IDENTITY(1,1) PRIMARY KEY,
    HocSinhId INT NOT NULL,
    MonHocId INT NOT NULL,
    HocKy TINYINT NOT NULL,
    NamHoc NVARCHAR(20) NOT NULL,
    DiemMieng FLOAT NULL,
    Diem15Phut FLOAT NULL,
    Diem1Tiet FLOAT NULL,
    DiemGiuaKy FLOAT NULL,
    DiemCuoiKy FLOAT NULL,
    CONSTRAINT CK_Diem_HocKy CHECK (HocKy IN (1,2)),
    CONSTRAINT FK_Diem_HocSinh FOREIGN KEY (HocSinhId) REFERENCES HocSinh(HocSinhId),
    CONSTRAINT FK_Diem_MonHoc FOREIGN KEY (MonHocId) REFERENCES MonHoc(MonHocId),
    CONSTRAINT UQ_Diem UNIQUE (HocSinhId, MonHocId, HocKy, NamHoc)
);

CREATE TABLE TaiKhoan (
    TaiKhoanId INT IDENTITY(1,1) PRIMARY KEY,
    Username NVARCHAR(50) NOT NULL,
    Password NVARCHAR(100) NOT NULL,
    Role TINYINT NOT NULL CONSTRAINT DF_TaiKhoan_Role DEFAULT 0,
    HocSinhId INT NULL,
    GiaoVienId INT NULL,
    IsActive BIT NOT NULL CONSTRAINT DF_TaiKhoan_IsActive DEFAULT 1,
    CONSTRAINT UQ_TaiKhoan_Username UNIQUE (Username),
    CONSTRAINT FK_TaiKhoan_HocSinh FOREIGN KEY (HocSinhId) REFERENCES HocSinh(HocSinhId),
    CONSTRAINT FK_TaiKhoan_GiaoVien FOREIGN KEY (GiaoVienId) REFERENCES GiaoVien(GiaoVienId),
    CONSTRAINT CK_TaiKhoan_Role CHECK (Role IN (0,1,2)),
    CONSTRAINT CK_TaiKhoan_OneOwner CHECK (
        (HocSinhId IS NULL AND GiaoVienId IS NULL)
        OR (HocSinhId IS NOT NULL AND GiaoVienId IS NULL)
        OR (HocSinhId IS NULL AND GiaoVienId IS NOT NULL)
    )
);

CREATE TABLE LichSuLop (
    LichSuLopId INT IDENTITY(1,1) PRIMARY KEY,
    HocSinhId INT NOT NULL,
    LopId INT NOT NULL,
    NamHoc NVARCHAR(20) NOT NULL,
    GhiChu NVARCHAR(200) NULL,
    NgayChuyen DATETIME NOT NULL CONSTRAINT DF_LichSuLop_NgayChuyen DEFAULT GETDATE(),
    CONSTRAINT FK_LichSuLop_HocSinh FOREIGN KEY (HocSinhId) REFERENCES HocSinh(HocSinhId),
    CONSTRAINT FK_LichSuLop_Lop FOREIGN KEY (LopId) REFERENCES Lop(LopId)
);

CREATE INDEX IX_HocSinh_LopId ON HocSinh(LopId);
CREATE INDEX IX_HocSinh_MaHocSinh ON HocSinh(MaHocSinh);
CREATE INDEX IX_Diem_HocSinhId ON Diem(HocSinhId);
CREATE INDEX IX_Diem_MonHocId ON Diem(MonHocId);
CREATE INDEX IX_PhanCong_LopId ON PhanCong(LopId);
CREATE INDEX IX_PhanCong_GiaoVienId ON PhanCong(GiaoVienId);
CREATE INDEX IX_TaiKhoan_Username ON TaiKhoan(Username);
CREATE INDEX IX_LichSuLop_HocSinhId ON LichSuLop(HocSinhId);
GO

-- 1. Bảng Lop
INSERT INTO Lop (TenLop, Khoi, NamHoc)
VALUES 
(N'10A1', 10, N'2024-2025'),
(N'11A1', 11, N'2024-2025'),
(N'12A1', 12, N'2024-2025');
GO

-- 2. Bảng MonHoc
INSERT INTO MonHoc (MaMH, TenMH, HeSo)
VALUES
(N'MH01', N'Toán', 2),
(N'MH02', N'Ngữ văn', 2),
(N'MH03', N'Tiếng Anh', 1);
GO

-- 3. Bảng GiaoVien
INSERT INTO GiaoVien (MaGiaoVien, HoTen, NgaySinh, GioiTinh, DiaChi, SoDienThoai, Email, ChuyenMon, TrinhDo, NgayVaoTruong)
VALUES
(N'GV001', N'Nguyễn Văn An', '1985-05-12', N'Nam', N'Hà Nội', '0901234567', 'an.nguyen@school.edu.vn', N'Toán', N'Thạc sĩ', '2010-09-01'),
(N'GV002', N'Trần Thị Bình', '1988-08-20', N'Nữ', N'Hải Phòng', '0902345678', 'binh.tran@school.edu.vn', N'Ngữ văn', N'Thạc sĩ', '2012-09-01'),
(N'GV003', N'Lê Văn Cường', '1990-03-15', N'Nam', N'Hải Dương', '0903456789', 'cuong.le@school.edu.vn', N'Tiếng Anh', N'Cử nhân', '2015-09-01');
GO

-- 4. Bảng HocSinh
INSERT INTO HocSinh 
(MaHocSinh, HoTen, NgaySinh, GioiTinh, DiaChi, SoDienThoai, Email, LopId, NgayNhapHoc, HoTenCha, HoTenMe, SDTChaMe)
VALUES
(N'HS001', N'Nguyễn Minh Anh', '2008-01-10', N'Nam', N'Hải Phòng', '0911111111', 'minhanh@gmail.com', 1, '2024-09-01', N'Nguyễn Văn A', N'Trần Thị A', '0988888888'),
(N'HS002', N'Trần Thu Hà', '2007-06-22', N'Nữ', N'Hải Phòng', '0922222222', 'thuha@gmail.com', 2, '2024-09-01', N'Trần Văn B', N'Lê Thị B', '0977777777'),
(N'HS003', N'Lê Quốc Bảo', '2006-11-05', N'Nam', N'Hải Dương', '0933333333', 'quocbao@gmail.com', 3, '2024-09-01', N'Lê Văn C', N'Phạm Thị C', '0966666666');
GO

-- 5. Bảng PhanCong
INSERT INTO PhanCong (LopId, MonHocId, GiaoVienId, NamHoc, HocKy, IsChuNhiem)
VALUES
(1, 1, 1, N'2024-2025', 1, 1), -- GV chủ nhiệm lớp 10A1
(2, 2, 2, N'2024-2025', 1, 1), -- GV chủ nhiệm lớp 11A1
(3, 3, 3, N'2024-2025', 1, 1); -- GV chủ nhiệm lớp 12A1
GO

-- 6. Bảng Diem
INSERT INTO Diem 
(HocSinhId, MonHocId, HocKy, NamHoc, DiemMieng, Diem15Phut, Diem1Tiet, DiemGiuaKy, DiemCuoiKy)
VALUES
(1, 1, 1, N'2024-2025', 8.0, 7.5, 8.5, 8.0, 8.5),
(2, 2, 1, N'2024-2025', 7.0, 7.5, 7.0, 7.5, 8.0),
(3, 3, 1, N'2024-2025', 9.0, 8.5, 9.0, 8.5, 9.5);
GO

-- 7. Bảng TaiKhoan
-- Role: 0 = Admin, 1 = Giáo viên, 2 = Học sinh
INSERT INTO TaiKhoan (Username, Password, Role, HocSinhId, GiaoVienId)
VALUES
(N'admin', N'admin123', 0, NULL, NULL),
(N'gv_an', N'123456', 1, NULL, 1),
(N'hs_minh', N'123456', 2, 1, NULL);
GO

-- 8. Bảng LichSuLop
INSERT INTO LichSuLop (HocSinhId, LopId, NamHoc, GhiChu)
VALUES
(1, 1, N'2024-2025', N'Nhập học ban đầu'),
(2, 2, N'2024-2025', N'Nhập học ban đầu'),
(3, 3, N'2024-2025', N'Nhập học ban đầu');
GO