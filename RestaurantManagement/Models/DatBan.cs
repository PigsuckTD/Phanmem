using System;

namespace Phanmem.Models
{
    public class DatBan
    {
        public int MaDatBan { get; set; } // nếu có khóa chính tự tăng

        public int SoBan { get; set; } // số bàn (ví dụ: 5, 12, VIP1...)

        public string TenKH { get; set; }

        public string SDT { get; set; }

        public string KhuVuc { get; set; } // Cơ sở / Khu vực: Quận 1, Quận 7, Tầng 2...

        public DateTime NgayDat { get; set; }

        public string GioDat { get; set; } // "19:30", "20:00"...

        public int SoNguoi { get; set; }

        public string UuDai { get; set; } // optional

        public string GhiChu { get; set; } // optional

        public DateTime NgayTao { get; set; } = DateTime.Now;

        public string TrangThai { get; set; } = "Chờ xác nhận"; // Chờ xác nhận / Đã xác nhận / Đã hủy / Đã đến

        // Constructor đầy đủ (dùng khi tạo mới)
        public DatBan(int soBan, string tenKH, string sdt, string khuVuc,
                      DateTime ngayDat, string gioDat, int soNguoi,
                      string uuDai = null, string ghiChu = null)
        {
            SoBan = soBan;
            TenKH = tenKH;
            SDT = sdt;
            KhuVuc = khuVuc;
            NgayDat = ngayDat;
            GioDat = gioDat;
            SoNguoi = soNguoi;
            UuDai = uuDai;
            GhiChu = ghiChu;
        }

        // Constructor rỗng (dùng cho Entity Framework hoặc khi load từ DB)
        public DatBan()
        {
        }
    }
}