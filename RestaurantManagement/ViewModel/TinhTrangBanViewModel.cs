using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing.Text;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using QuanLyNhaHang.Models;
using QuanLyNhaHang.View;
using RestaurantManagement.Models;
using TinhTrangBan.Models;
using System.Data.SqlClient;
using System.Configuration;
using System.Data;
using System.Windows.Forms;
using OfficeOpenXml.ConditionalFormatting;
using System.Diagnostics;
using System.Reflection.Metadata.Ecma335;
using QuanLyNhaHang.DataProvider;
using Org.BouncyCastle.Math;
using iTextSharp.text.pdf;
using iTextSharp.text;
using Documment = iTextSharp.text.Document;
using System.IO;
using Phanmem.ViewModel;
using System.Windows.Threading; // Thêm để dùng Dispatcher.CurrentDispatcher

namespace QuanLyNhaHang.ViewModel
{
    public class TinhTrangBanViewModel : BaseViewModel
    {
        string connectstring = ConfigurationManager.ConnectionStrings["QuanLyNhaHang"].ConnectionString;
        public TinhTrangBanViewModel()
        {
            StatusOfTableCommand = new RelayCommand<BanAnViewModel>((p) => true, (p) => GetStatusOfTable(p.SoBan));
            GetPaymentCommand = new RelayCommand<BanAnViewModel>((p) => true, (p) => Payment());
            GetSwitchTableCommand = new RelayCommand<string>((p) => true, (p) => SwitchTable());
            LoadTables();
            LoadTableStatus();
            LoadEmptyTables();
            // ĐĂNG KÝ NGHE SỰ KIỆN
            AppEvents.BookingUpdated += OnBookingUpdated;
            ApplyDiscountCommand = new RelayCommand<object>((p) => true, (p) => ApplyDiscount());
        }
        #region attributes
        public ICommand ApplyDiscountCommand { get; set; }
        private ObservableCollection<BanAnViewModel> _tables = new ObservableCollection<BanAnViewModel>();
        private ObservableCollection<SelectedMenuItems> _selectedItems = new ObservableCollection<SelectedMenuItems>();
        private ObservableCollection<string> _emptytables = new ObservableCollection<string>();
        private string titleofbill = "";
        private decimal dec_sumofbill = 0;
        private string sumofbill = "0 VND";
        // Tạm tính = tổng tiền món hiện tại
        public decimal TempTotal => Dec_sumofbill;
        public string TempTotalText => String.Format("{0:0,0 VND}", TempTotal);

        // % giảm (nhân viên nhập, ví dụ 20 nghĩa là 20%)
        private decimal discountPercent;
        public decimal DiscountPercent
        {
            get => discountPercent;
            set
            {
                discountPercent = value;
                OnPropertyChanged();
            }
        }

        // Số tiền giảm (VNĐ)
        private decimal discountAmount;
        public decimal DiscountAmount
        {
            get => discountAmount;
            set
            {
                discountAmount = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(DiscountAmountText));
                OnPropertyChanged(nameof(FinalAmount));
                OnPropertyChanged(nameof(FinalAmountText));
            }
        }

        public string DiscountAmountText => String.Format("{0:0,0 VND}", DiscountAmount);

        // Thành tiền sau giảm
        public decimal FinalAmount => TempTotal - DiscountAmount;
        public string FinalAmountText => String.Format("{0:0,0 VND}", FinalAmount);

        // Hàm tính giảm giá từ % nhập vào
        public void ApplyDiscount()
        {
            if (DiscountPercent <= 0)
                DiscountAmount = 0;
            else
                DiscountAmount = Math.Round(TempTotal * DiscountPercent / 100m, 0);

            // Nếu muốn label "Tổng cộng" hiển thị THÀNH TIỀN
            SumofBill = FinalAmountText;
        }

        private string selectedtable = "";
        int IDofPaidTable = 0;
        bool isNull = false;
        #endregion
        #region properties
        public ObservableCollection<BanAnViewModel> Tables
        {
            get => _tables;
            set { _tables = value; OnPropertyChanged(); }
        }
        public ObservableCollection<SelectedMenuItems> SelectedItems { get { return _selectedItems; } set { _selectedItems = value; } }
        public ObservableCollection<string> EmptyTables { get { return _emptytables; } set { _emptytables = value; } }
        public string TitleOfBill
        {
            get { return titleofbill; }
            set { titleofbill = value; OnPropertyChanged(); }
        }
        public decimal Dec_sumofbill
        {
            get { return dec_sumofbill; }
            set { dec_sumofbill = value; OnPropertyChanged(); }
        }
        public string SumofBill
        {
            get { return sumofbill; }
            set { sumofbill = value; OnPropertyChanged(); }
        }
        public string SelectedTable
        {
            get { return selectedtable; }
            set { selectedtable = value; OnPropertyChanged(); }
        }
        #endregion
        #region commands
        public ICommand StatusOfTableCommand { get; set; }
        public ICommand GetPaymentCommand { get; set; }
        public ICommand GetSwitchTableCommand { get; set; }
        #endregion
        #region methods
        public void LoadTables()
        {
            _tables.Clear(); // Xóa cũ nếu có
            for (int i = 1; i <= 15; i++)
            {
                _tables.Add(new BanAnViewModel
                {
                    SoBan = i,
                    TrangThai = "Trống" // mặc định
                });
            }
            OnPropertyChanged(nameof(Tables)); // báo UI reload lần đầu
        }
        public void LoadEmptyTables()
        {
            string numoftable;
            using (SqlConnection con = new SqlConnection(connectstring))
            {
                con.Open();
                SqlCommand cmd = new SqlCommand();
                cmd.Connection = con;
                cmd.CommandType = CommandType.Text;
                cmd.CommandText = "Select SoBan from BAN where TrangThai = N'Có thể sử dụng'";
                SqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    try
                    {
                        numoftable = reader.GetInt16(0).ToString();
                        _emptytables.Add(numoftable);
                        EmptyTables = _emptytables;
                    }
                    catch
                    {
                        numoftable = "";
                    }
                }
                con.Close();
            }
        }
        public void LoadTableStatus()
        {
            foreach (var ban in _tables)
            {
                string tablestatus = TinhTrangBanDP.Flag.LoadEachTableStatus(ban.SoBan);
                bool coDatBan = KiemTraBanDaDat(ban.SoBan);
                if (tablestatus == "Có thể sử dụng")
                {
                    if (coDatBan)
                        ban.TrangThai = "Đã đặt"; // Tự đổi màu thành Orange
                    else
                        ban.TrangThai = "Trống"; // Tự đổi màu thành LightGreen
                }
                else
                {
                    ban.TrangThai = "Có Khách"; // Tự đổi màu thành Red
                }
            }
        }
        private bool KiemTraBanDaDat(int soBan)
        {
            using (SqlConnection con = new SqlConnection(connectstring))
            {
                con.Open();
                SqlCommand cmd = new SqlCommand();
                cmd.Connection = con;
                cmd.CommandType = CommandType.Text;
                cmd.CommandText = @"
            SELECT COUNT(*)
            FROM CHITIETDATBAN
            WHERE SoBan = @SoBan
                AND NgayDat = CAST(GETDATE() AS DATE)
                AND TrangThai = N'Đã đặt'
                AND DATEADD(MINUTE, 30,
                    CAST(NgayDat AS DATETIME) + CAST(CAST(GioDat AS TIME) AS DATETIME)
                ) > GETDATE()";
                cmd.Parameters.AddWithValue("@SoBan", soBan);
                int count = (int)cmd.ExecuteScalar();
                return count > 0;
            }
        }
        public void DisplayBill(int BillID)
        {
            SelectedItems.Clear();
            Dec_sumofbill = 0;
            //Reset giảm giá khi load bill mới
            DiscountPercent = 0;
            DiscountAmount = 0;

            string FoodName;
            decimal Price;
            int Quantity;
            using (SqlConnection con = new SqlConnection(connectstring))
            {
                con.Open();
                SqlCommand cmd = new SqlCommand();
                cmd.Connection = con;
                cmd.CommandType = CommandType.Text;
                cmd.CommandText = "Select TenMon, SoLuong, Gia * SoLuong " +
                    "from CTHD inner join MENU on CTHD.MaMon = MENU.MaMon " +
                    "where CTHD.SoHD = @SOHD";
                cmd.Parameters.AddWithValue("@SOHD", BillID);
                SqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    try
                    {
                        FoodName = reader.GetString(0);
                        Quantity = reader.GetInt16(1);
                        Price = reader.GetDecimal(2);
                        SelectedMenuItems selected = new SelectedMenuItems(FoodName, Price, Quantity);
                        SelectedItems.Add(selected);
                        Dec_sumofbill += Price;
                        SumofBill = String.Format("{0:0,0 VND}", Dec_sumofbill);
                    }
                    catch
                    {
                        FoodName = "";
                        Quantity = 0;
                        Price = 0;
                    }
                }

                OnPropertyChanged(nameof(TempTotal));
                OnPropertyChanged(nameof(TempTotalText));
                OnPropertyChanged(nameof(FinalAmount));
                OnPropertyChanged(nameof(FinalAmountText));

                con.Close();
            }
        }
        public void GetStatusOfTable(int soBan)
        {
            var ban = _tables.FirstOrDefault(t => t.SoBan == soBan);
            if (ban == null) return;
            if (ban.TrangThai == "Trống")
            {
                // Bàn trống - không làm gì
            }
            else if (ban.TrangThai == "Đã đặt")
            {
                // Bàn đã đặt - Hiển thị thông tin đặt bàn
                HienThiThongTinDatBan(soBan);
            }
            else if (ban.TrangThai == "Có Khách")
            {
                // Bàn đang có khách - Hiển thị hóa đơn
                TitleOfBill = $"Bàn {soBan}";
                int billId = TinhTrangBanDP.Flag.LoadBill(soBan);
                DisplayBill(billId);
                IDofPaidTable = soBan;
            }
        }
        // Hàm hiển thị thông tin đặt bàn
        private void HienThiThongTinDatBan(int soBan)
        {
            using (SqlConnection con = new SqlConnection(connectstring))
            {
                con.Open();
                SqlCommand cmd = new SqlCommand();
                cmd.Connection = con;
                cmd.CommandType = CommandType.Text;
                // Lấy thông tin đặt bàn mới nhất của bàn này
                cmd.CommandText = @"
                    SELECT TOP 1
                        MaDatBan, TenKH, SDT, NgayDat, GioDat, SoNguoi, UuDai, GhiChu, TrangThai
                    FROM CHITIETDATBAN
                    WHERE SoBan = @SoBan
                        AND NgayDat = CAST(GETDATE() AS DATE)
                        AND TrangThai = N'Đã đặt'
                        AND DATEADD(MINUTE, 30,
                            CAST(NgayDat AS DATETIME) + CAST(CAST(GioDat AS TIME) AS DATETIME)
                        ) > GETDATE()
                    ORDER BY MaDatBan DESC";
                cmd.Parameters.AddWithValue("@SoBan", soBan);
                SqlDataReader reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    int maDatBan = reader.GetInt32(0);
                    string tenKH = reader.GetString(1);
                    string sdt = reader.GetString(2);
                    DateTime ngayDat = reader.GetDateTime(3);
                    string gioDat = reader.GetString(4);
                    int soNguoi = reader.GetByte(5);
                    string uuDai = reader.IsDBNull(6) ? "" : reader.GetString(6);
                    string ghiChu = reader.IsDBNull(7) ? "" : reader.GetString(7);
                    reader.Close();
                    con.Close();
                    // Hiển thị popup thông tin đặt bàn
                    var confirmData = new QuanLyNhaHang.View.ThongTinDatBanData
                    {
                        MaDatBan = maDatBan,
                        SoBan = soBan,
                        TenKhach = tenKH,
                        SDT = sdt,
                        NgayDat = ngayDat,
                        GioDat = gioDat,
                        SoNguoi = soNguoi,
                        UuDai = uuDai,
                        GhiChu = ghiChu
                    };
                    var infoWindow = new QuanLyNhaHang.View.ThongTinDatBanWindow(confirmData);
                    infoWindow.ShowDialog();
                    // Xử lý kết quả từ user
                    if (infoWindow.ActionResult == "HUY")
                    {
                        HuyDatBan(maDatBan);
                        return; 
                    }
                    else if (infoWindow.ActionResult == "KHACH_DEN")
                    {
                        XacNhanKhachDen(soBan, maDatBan);
                        return;
                    }
                    // Reload trạng thái bàn
                    LoadTableStatus();
                }
                else
                {
                    reader.Close();
                    con.Close();
                    MyMessageBox msb = new MyMessageBox("Không tìm thấy thông tin đặt bàn!");
                    msb.ShowDialog();
                }
            }
        }
        // Hủy đặt bàn
        private void HuyDatBan(int maDatBan)
        {
            using (SqlConnection con = new SqlConnection(connectstring))
            {
                try
                {
                    con.Open();

                    // BƯỚC 1: Lấy SoBan từ MaDatBan trước khi hủy
                    SqlCommand cmdGetBan = new SqlCommand("SELECT SoBan FROM CHITIETDATBAN WHERE MaDatBan = @MaDatBan", con);
                    cmdGetBan.Parameters.AddWithValue("@MaDatBan", maDatBan);
                    object result = cmdGetBan.ExecuteScalar();

                    if (result == null || result == DBNull.Value)
                        return; // không tìm thấy → thoát luôn

                    int soBan = Convert.ToInt32(result);

                    // BƯỚC 2: Hủy đặt bàn
                    SqlCommand cmd = new SqlCommand("UPDATE CHITIETDATBAN SET TrangThai = N'Đã hủy' WHERE MaDatBan = @MaDatBan", con);
                    cmd.Parameters.AddWithValue("@MaDatBan", maDatBan);
                    cmd.ExecuteNonQuery();

                    // BƯỚC 3: QUAN TRỌNG NHẤT – ĐỔI MÀU BÀN THÀNH XANH NGAY
                    RefreshSingleTable(soBan);

                    // BƯỚC 4: Cập nhật lại danh sách bàn trống
                    LoadEmptyTables();

                    MyMessageBox msb = new MyMessageBox("Đã hủy đặt bàn thành công!");
                    msb.ShowDialog();
                }
                catch (Exception ex)
                {
                    MyMessageBox msb = new MyMessageBox("Lỗi khi hủy đặt bàn:\n" + ex.Message);
                    msb.ShowDialog();
                }
            }
        }
        // Xác nhận khách đến → Chuyển bàn sang trạng thái "Trống"
        private void XacNhanKhachDen(int soBan, int maDatBan)
        {
            using (SqlConnection con = new SqlConnection(connectstring))
            {
                try
                {
                    con.Open();

                    using (SqlCommand cmd = new SqlCommand())
                    {
                        cmd.Connection = con;
                        cmd.CommandType = CommandType.Text;

                        // 1. Cập nhật trạng thái chi tiết đặt bàn thành "Đã nhận bàn"
                        cmd.CommandText = "UPDATE CHITIETDATBAN SET TrangThai = N'Đã nhận bàn' WHERE MaDatBan = @MaDatBan";
                        cmd.Parameters.AddWithValue("@MaDatBan", maDatBan);
                        cmd.ExecuteNonQuery();
                        cmd.Parameters.Clear();

                        // 2. Cập nhật trạng thái bàn về "Trống" (sẵn sàng cho khách ngồi luôn)
                        cmd.CommandText = "UPDATE BAN SET TrangThai = N'Trống' WHERE SoBan = @SoBan";
                        cmd.Parameters.AddWithValue("@SoBan", soBan);
                        cmd.ExecuteNonQuery();
                    }

                    RefreshSingleTable(soBan);
                    LoadEmptyTables();
                    // Thông báo thành công
                    MyMessageBox msb = new MyMessageBox("Khách đã nhận bàn thành công!\nBàn đã sẵn sàng để phục vụ.");
                    msb.ShowDialog();
                }
                catch (Exception ex)
                {
                    MyMessageBox msb = new MyMessageBox("Lỗi khi xác nhận khách đến:\n" + ex.Message);
                    msb.ShowDialog();
                }
            }
        }
        public void PrintBill(int BillID, int TableID)
        {
            using (SqlConnection con = new SqlConnection(connectstring))
            {
                con.Open();
                string strQuery = "Select TenMon, SoLuong, Gia * SoLuong " +
                    "from CTHD inner join MENU on CTHD.MaMon = MENU.MaMon " +
                    "where CTHD.SoHD = " + BillID;
                SqlCommand cmd = new SqlCommand();
                cmd.Connection = con;
                cmd.CommandType = CommandType.Text;
                cmd.CommandText = strQuery;
                SqlDataReader reader = cmd.ExecuteReader();
                List<string> ten = new List<string>();
                List<string> soluong = new List<string>();
                List<string> gia = new List<string>();
                while (reader.Read())
                {
                    ten.Add(reader.GetString(0));
                    soluong.Add(reader.GetInt16(1).ToString());
                    gia.Add(reader.GetDecimal(2).ToString());
                }
                if (ten.Count > 0)
                {
                    DisplayBill(BillID);
                    MyMessageBox yesno = new MyMessageBox("Bạn có muốn in hóa đơn?", true);
                    yesno.ShowDialog();
                    if (yesno.ACCEPT())
                    {
                        SaveFileDialog sfd = new SaveFileDialog();
                        sfd.Filter = "PDF (*.pdf)|*.pdf";
                        sfd.FileName = "Mã hóa đơn " + BillID.ToString() + " ngày " + DateTime.Now.Day + "-" + DateTime.Now.Month + "-" + DateTime.Now.Year;
                        if (sfd.ShowDialog() == DialogResult.OK)
                        {
                            if (File.Exists(sfd.FileName))
                            {
                                try
                                {
                                    File.Delete(sfd.FileName);
                                }
                                catch (IOException ex)
                                {
                                    MyMessageBox msb = new MyMessageBox("Đã có lỗi xảy ra!");
                                    msb.ShowDialog();
                                }
                            }
                            try
                            {
                                PdfPTable pdfTable = new PdfPTable(3);
                                pdfTable.DefaultCell.Padding = 3;
                                pdfTable.WidthPercentage = 100;
                                pdfTable.HorizontalAlignment = Element.ALIGN_MIDDLE;
                                BaseFont bf = BaseFont.CreateFont(Environment.GetEnvironmentVariable("windir") + @"\fonts\TIMES.TTF", BaseFont.IDENTITY_H, true);
                                Font f = new Font(bf, 16, Font.NORMAL);
                                PdfPCell cell = new PdfPCell(new Phrase("Tên món", f));
                                pdfTable.AddCell(cell);
                                cell = new PdfPCell(new Phrase("Số lượng", f));
                                pdfTable.AddCell(cell);
                                cell = new PdfPCell(new Phrase("Giá", f));
                                pdfTable.AddCell(cell);
                                for (int i = 0; i < ten.Count; i++)
                                {
                                    pdfTable.AddCell(new Phrase(ten[i], f));
                                    pdfTable.AddCell(new Phrase(soluong[i], f));
                                    pdfTable.AddCell(new Phrase(gia[i], f));
                                }

                                // ==== LẤY TẠM TÍNH & GIẢM GIÁ TỪ HOADON ====
                                decimal triGia = 0;
                                decimal giamGia = 0;
                                using (SqlConnection con2 = new SqlConnection(connectstring))
                                {
                                    con2.Open();
                                    SqlCommand cmdBill = new SqlCommand(@"
                                SELECT ISNULL(TriGia,0), ISNULL(GiamGia,0)
                                FROM HOADON
                                WHERE SoHD = @SoHD", con2);
                                    cmdBill.Parameters.AddWithValue("@SoHD", BillID);
                                    using (SqlDataReader r = cmdBill.ExecuteReader())
                                    {
                                        if (r.Read())
                                        {
                                            triGia = r.GetDecimal(0);
                                            giamGia = r.GetDecimal(1);
                                        }
                                    }
                                }
                                decimal thanhTien = triGia - giamGia;

                                using (FileStream stream = new FileStream(sfd.FileName, FileMode.Create))
                                {
                                    Document pdfDoc = new Document(PageSize.A4, 50f, 50f, 40f, 40f);
                                    PdfWriter.GetInstance(pdfDoc, stream);
                                    pdfDoc.Open();
                                    pdfDoc.Add(new Paragraph(" HÓA ĐƠN ", f));
                                    pdfDoc.Add(new Paragraph(" "));
                                    pdfDoc.Add(new Paragraph("Số bàn: " + TableID.ToString() + " Mã hóa đơn: " + BillID.ToString(), f));
                                    pdfDoc.Add(new Paragraph("Thời gian: " + DateTime.Now.Day.ToString() + "/" + DateTime.Now.Month.ToString() + "/" + DateTime.Now.Year.ToString() + " " + DateTime.Now.TimeOfDay.ToString(), f));
                                    pdfDoc.Add(new Paragraph(" "));
                                    pdfDoc.Add(pdfTable);
                                    pdfDoc.Add(new Paragraph(" "));

                                    // In Tạm tính / Giảm giá / Thành tiền
                                    pdfDoc.Add(new Paragraph("Tạm tính: " + String.Format("{0:0,0 VND}", triGia), f));
                                    if (giamGia > 0)
                                    {
                                        pdfDoc.Add(new Paragraph("Giảm giá: " + String.Format("{0:0,0 VND}", giamGia), f));
                                    }
                                    pdfDoc.Add(new Paragraph("Thành tiền: " + String.Format("{0:0,0 VND}", thanhTien), f));

                                    pdfDoc.Add(new Paragraph(" "));
                                    pdfDoc.Add(new Paragraph(" HẸN GẶP LẠI QUÝ KHÁCH", f));
                                    pdfDoc.Close();
                                    stream.Close();
                                }
                                MyMessageBox mess = new MyMessageBox("In thành công!");
                                mess.ShowDialog();
                            }
                            catch (Exception ex)
                            {
                                MyMessageBox msb = new MyMessageBox("Đã có lỗi xảy ra!");
                                msb.ShowDialog();
                            }
                        }
                    }
                    else
                    {
                        DisplayBill(BillID);
                    }
                }
                else
                {
                    MyMessageBox mess = new MyMessageBox("Không tồn tại hóa đơn!");
                    mess.ShowDialog();
                }
            }
        }

        public void Payment()
        {
            var ban = _tables.FirstOrDefault(b => b.SoBan == IDofPaidTable);
            if (ban != null)
            {
                //Lấy mã hóa đơn của bàn hiện tại
                int billId = TinhTrangBanDP.Flag.LoadBill(ban.SoBan);

                //1. Lưu tạm tính và giảm giá vào HOADON    
                using (SqlConnection con = new SqlConnection(connectstring))
                {
                    con.Open();
                    SqlCommand cmd = new SqlCommand(@"
                        UPDATE dbo.HOADON
                        SET TriGia = @TriGia,
                            GiamGia = @GiamGia,
                            NgayHD = GETDATE()
                        WHERE SoHD = @SoHD", con);

                        cmd.Parameters.AddWithValue("@TriGia", TempTotal);
                        cmd.Parameters.AddWithValue("@GiamGia", DiscountAmount);
                        cmd.Parameters.AddWithValue("@SoHD", billId);
                        cmd.ExecuteNonQuery();
                }
                //2. Cập nhật trạng thái bàn + trạng thái hóa đơn
                ban.TrangThai = "Trống";
                TinhTrangBanDP.Flag.UpdateTable(ban.SoBan, true);
                TinhTrangBanDP.Flag.UpdateBillStatus(billId);

                //3. In hóa đơn với dữ liệu đã có GiamGia
                PrintBill(billId, ban.SoBan);

                //4. Reset UI
                Dec_sumofbill = 0;
                OnPropertyChanged(nameof(TempTotal));
                OnPropertyChanged(nameof(TempTotalText));
                OnPropertyChanged(nameof(FinalAmount));
                OnPropertyChanged(nameof(FinalAmountText));
                DiscountPercent = 0;
                DiscountAmount = 0;
                SumofBill = String.Format("{0:0,0 VND}", Dec_sumofbill);
                SelectedItems.Clear();
                TitleOfBill = "";
                MyMessageBox msb = new MyMessageBox("Thanh toán thành công!");
                msb.Show();
            }
        }
        public void SwitchTable()
        {
            var ban = _tables.FirstOrDefault(b => b.SoBan == IDofPaidTable);
            if (ban != null)
            {
                if (SelectedTable == "")
                {
                    MyMessageBox msb1 = new MyMessageBox("Vui lòng chọn bàn để chuyển đến trong danh sách bàn trống!");
                    msb1.Show();
                    isNull = true;
                    return;
                }
                ban.TrangThai = "Trống";
                TinhTrangBanDP.Flag.UpdateTable(ban.SoBan, true);
                int targetBan = int.Parse(SelectedTable);
                TinhTrangBanDP.Flag.SwitchTable(targetBan, TinhTrangBanDP.Flag.LoadBill(ban.SoBan));
                TinhTrangBanDP.Flag.UpdateTable(targetBan, false);
                Dec_sumofbill = 0;
                SumofBill = String.Format("{0:0,0 VND}", Dec_sumofbill);
                SelectedItems.Clear();
                TitleOfBill = "";
                MyMessageBox msb2 = new MyMessageBox("Đã chuyển bàn thành công!");
                msb2.Show();
            }
            if (IDofPaidTable == 0)
            {
                MyMessageBox msb1 = new MyMessageBox("Vui lòng ấn chọn 1 bàn cần chuyển trước khi nhấn nút Chuyển bàn!");
                msb1.Show();
                isNull = true;
            }
            if (!isNull)
            {
                var targetBan = _tables.FirstOrDefault(b => b.SoBan == int.Parse(SelectedTable));
                if (targetBan != null)
                {
                    targetBan.TrangThai = "Có Khách";
                }
            }
            EmptyTables.Clear();
            LoadEmptyTables();
        }
        // Handler để nhận update khi có booking mới (DatBanViewModel sẽ gọi AppEvents.RaiseBookingUpdated)
        private void OnBookingUpdated(int soBan, string trangThaiMoi, int maDatBan)
        {
            Dispatcher.CurrentDispatcher.Invoke(() =>
            {
                var ban = _tables.FirstOrDefault(t => t.SoBan == soBan);
                if (ban != null)
                {
                    if (trangThaiMoi.Contains("Đã đặt"))
                        ban.TrangThai = "Đã đặt";
                    else if (trangThaiMoi.Contains("khách") || trangThaiMoi.Contains("sử dụng") || trangThaiMoi.Contains("nhận"))
                        ban.TrangThai = "Có Khách";
                    else
                        ban.TrangThai = "Trống";
                }
                else
                {
                    LoadTableStatus(); // fallback
                }
            });
        }

        /// <summary>
        /// Cập nhật lại trạng thái chỉ đúng 1 bàn – dùng khi khách đến, hủy đặt bàn, v.v.
        /// Bàn sẽ chuyển màu xanh lá ngay lập tức nếu không còn đặt bàn hợp lệ nào.
        /// </summary>
        private void RefreshSingleTable(int soBan)
        {
            var ban = _tables.FirstOrDefault(t => t.SoBan == soBan);
            if (ban == null) return;

            string dbStatus = TinhTrangBanDP.Flag.LoadEachTableStatus(soBan);

            // Sửa lại điều kiện: "Trống" và "Có thể sử dụng" đều là bàn chưa có khách
            if (dbStatus == "Trống" || dbStatus == "Có thể sử dụng")
            {
                bool conDatHopLe = KiemTraBanDaDat(soBan);
                ban.TrangThai = conDatHopLe ? "Đã đặt" : "Trống";
            }
            else
            {
                ban.TrangThai = "Có Khách";
            }
        }
        #endregion
    }
}