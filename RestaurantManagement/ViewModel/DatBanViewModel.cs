using System;
using System.Data;
using System.Data.SqlClient;
using System.Windows.Input;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Media;
using QuanLyNhaHang.Models;
using System.Configuration;
using System.Windows;
using Phanmem.ViewModel;
using System.Collections.Generic;

namespace QuanLyNhaHang.ViewModel
{
    public class DatBanViewModel : BaseViewModel
    {
        private string strCon = ConfigurationManager.ConnectionStrings["QuanLyNhaHang"].ConnectionString;
        private SqlConnection sqlCon = null;

        // === CÁC PROPERTY ===
        private string _TenKhach;
        public string TenKhach { get => _TenKhach; set { _TenKhach = value; OnPropertyChanged(); } }

        private string _SDT;
        public string SDT { get => _SDT; set { _SDT = value; OnPropertyChanged(); } }

        private DateTime _NgayDat = DateTime.Today;
        public DateTime NgayDat
        {
            get => _NgayDat;
            set
            {
                _NgayDat = value;
                OnPropertyChanged();
                LoadBanTrong(); // Tự động cập nhật bàn trống khi đổi ngày
            }
        }

        private string _GioDat;
        public string GioDat
        {
            get => _GioDat;
            set
            {
                _GioDat = value;
                OnPropertyChanged();
                LoadBanTrong(); // Tự động cập nhật khi đổi giờ
            }
        }

        private int _SoNguoi = 2;
        public int SoNguoi { get => _SoNguoi; set { _SoNguoi = value; OnPropertyChanged(); } }

        private string _UuDai;
        public string UuDai { get => _UuDai; set { _UuDai = value; OnPropertyChanged(); } }
        public List<string> UuDaiList { get; set; } = new List<string>
        {
            "Uư đãi sinh nhật 10% hóa đơn",
            "Có mã ưu đãi riêng",
            "Đầy tiền, không cần ưu đãi"
        };


        private string _GhiChu;
        public string GhiChu { get => _GhiChu; set { _GhiChu = value; OnPropertyChanged(); } }

        private int _SoBan;
        public int SoBan { get => _SoBan; set { _SoBan = value; OnPropertyChanged(); } }

        // Danh sách giờ từ 6h00 đến 23h30
        public ObservableCollection<string> TimeOptions { get; set; }

        // Danh sách bàn + trạng thái
        private ObservableCollection<BanItem> _DanhSachBan;
        public ObservableCollection<BanItem> DanhSachBan
        {
            get => _DanhSachBan;
            set { _DanhSachBan = value; OnPropertyChanged(); }
        }

        private BanItem _BanDuocChon;
        public BanItem BanDuocChon
        {
            get => _BanDuocChon;
            set
            {
                _BanDuocChon = value;
                if (value != null)
                    SoBan = value.SoBan;
                OnPropertyChanged();
            }
        }

        public ICommand DatBanCommand { get; set; }

        public DatBanViewModel()
        {
            TimeOptions = new ObservableCollection<string>();
            for (int h = 6; h <= 23; h++)
            {
                TimeOptions.Add($"{h:D2}:00");
                TimeOptions.Add($"{h:D2}:30");
            }

            DanhSachBan = new ObservableCollection<BanItem>();

            // Load tất cả bàn ngay từ đầu
            LoadTatCaBan();

            DatBanCommand = new RelayCommand<object>(CanDatBan, ExecuteDatBan);

            // Tự động hủy đặt bàn quá giờ mỗi 5 phút
            StartAutoHuyDatBanQuaGio();
        }

        private System.Windows.Threading.DispatcherTimer _autoHuyTimer;

        private void StartAutoHuyDatBanQuaGio()
        {
            _autoHuyTimer = new System.Windows.Threading.DispatcherTimer();
            _autoHuyTimer.Interval = TimeSpan.FromMinutes(5); // Chạy mỗi 5 phút
            _autoHuyTimer.Tick += (s, e) => AutoHuyDatBanQuaGio();
            _autoHuyTimer.Start();

            // Chạy ngay lần đầu
            AutoHuyDatBanQuaGio();
        }

        private void AutoHuyDatBanQuaGio()
        {
            OpenConnect();
            try
            {
                // Hủy các đặt bàn quá 30 phút
                string sql = @"
                    UPDATE dbo.CHITIETDATBAN
                    SET TrangThai = N'Quá giờ'
                    WHERE TrangThai = N'Đã đặt'
                        AND NgayDat = CAST(GETDATE() AS DATE)
                        AND DATEADD(MINUTE, 30, 
                            CAST(NgayDat AS DATETIME) + CAST(CAST(GioDat AS TIME) AS DATETIME)
                        ) < GETDATE()";

                using (var cmd = new SqlCommand(sql, sqlCon))
                {
                    int rowsAffected = cmd.ExecuteNonQuery();

                    if (rowsAffected > 0)
                    {
                        System.Diagnostics.Debug.WriteLine($"Đã tự động hủy {rowsAffected} đặt bàn quá giờ");
                        LoadBanTrong(); // Reload danh sách bàn
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Lỗi tự động hủy: {ex.Message}");
            }
            finally
            {
                CloseConnect();
            }
        }

        private bool CanDatBan(object obj)
        {
            return !string.IsNullOrWhiteSpace(TenKhach) &&
                   !string.IsNullOrWhiteSpace(SDT) &&
                   SoBan > 0 &&
                   !string.IsNullOrWhiteSpace(GioDat) &&
                   BanDuocChon?.TrangThai == "Trống"; // Chỉ cho đặt nếu bàn trống
        }

        private void ExecuteDatBan(object obj)
        {
            OpenConnect();
            try
            {
                using (var cmd = new SqlCommand("sp_DatBan_Insert", sqlCon))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@SoBan", SoBan);
                    cmd.Parameters.AddWithValue("@TenKH", TenKhach.Trim());
                    cmd.Parameters.AddWithValue("@SDT", SDT.Trim());
                    cmd.Parameters.AddWithValue("@NgayDat", NgayDat.Date);

                    // chuẩn hoá GioDat sang HH:mm
                    string gioDatFormatted = GioDat;
                    try
                    {
                        if (!string.IsNullOrWhiteSpace(GioDat))
                            gioDatFormatted = DateTime.ParseExact(GioDat, "H:mm", null).ToString("HH:mm");
                    }
                    catch
                    {
                        // thử parse HH:mm trực tiếp, nếu fail thì giữ nguyên (proc sẽ reject nếu format sai)
                    }

                    cmd.Parameters.AddWithValue("@GioDat", gioDatFormatted);
                    cmd.Parameters.AddWithValue("@SoNguoi", SoNguoi);
                    cmd.Parameters.AddWithValue("@UuDai", (object)UuDai ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@GhiChu", (object)GhiChu ?? DBNull.Value);

                    var output = new SqlParameter("@KetQua", SqlDbType.NVarChar, 400) { Direction = ParameterDirection.Output };
                    cmd.Parameters.Add(output);

                    // Nếu proc của bạn trả resultset (record), có thể đọc bằng reader.
                    // Nhưng ExecuteNonQuery vẫn cho ta output params, nên dùng ExecuteNonQuery.
                    cmd.ExecuteNonQuery();

                    string ketqua = output.Value?.ToString() ?? "Đã lưu!";

                    new MyMessageBox(ketqua).ShowDialog();

                    if (!string.IsNullOrEmpty(ketqua) && ketqua.ToLower().Contains("thành công"))
                    {
                        // Reset form
                        TenKhach = SDT = UuDai = GhiChu = "";
                        SoNguoi = 2;
                        GioDat = null;
                        BanDuocChon = null;
                        OnPropertyChanged(nameof(TenKhach));
                        OnPropertyChanged(nameof(SDT));
                        OnPropertyChanged(nameof(UuDai));
                        OnPropertyChanged(nameof(GhiChu));
                        OnPropertyChanged(nameof(SoNguoi));
                        OnPropertyChanged(nameof(GioDat));
                        OnPropertyChanged(nameof(BanDuocChon));

                        // Nếu cần reload để đồng bộ các view khác:
                        LoadBanTrong();
                    }
                }
            }
            catch (Exception ex)
            {
                new MyMessageBox("Lỗi: " + ex.Message).ShowDialog();
            }
            finally
            {
                CloseConnect();
            }
        }

        // Load tất cả bàn với trạng thái hiện tại từ DB
        private void LoadTatCaBan()
        {
            DanhSachBan.Clear();
            OpenConnect();
            try
            {
                // Query đơn giản lấy tất cả bàn
                string sql = "SELECT SoBan, TrangThai FROM dbo.BAN ORDER BY SoBan";
                using (var cmd = new SqlCommand(sql, sqlCon))
                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        int soBan = r.GetInt16(0);
                        string trangThaiDB = r.GetString(1);

                        // Chuyển đổi trạng thái từ DB
                        string trangThai = (trangThaiDB == "Đang sử dụng") ? "Có khách" : "Trống";

                        DanhSachBan.Add(new BanItem
                        {
                            SoBan = soBan,
                            TrangThai = trangThai
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                // Nếu lỗi DB, tạo danh sách bàn mặc định 1-15
                MessageBox.Show("Lỗi load bàn từ DB: " + ex.Message + "\nTạo danh sách mặc định...");
                for (int i = 1; i <= 15; i++)
                {
                    DanhSachBan.Add(new BanItem
                    {
                        SoBan = i,
                        TrangThai = "Trống"
                    });
                }
            }
            finally
            {
                CloseConnect();
            }
        }

        private void LoadBanTrong()
        {
            DanhSachBan.Clear();

            // Nếu chưa chọn ngày/giờ thì load tất cả bàn với trạng thái hiện tại
            if (NgayDat < DateTime.Today.Date || string.IsNullOrEmpty(GioDat))
            {
                LoadTatCaBan();
                return;
            }

            OpenConnect();
            try
            {
                // Query kiểm tra bàn đã đặt cho ngày/giờ cụ thể
                // Lấy thêm c.MaDatBan nếu có
                string sql = @"
                    SELECT b.SoBan,
                           b.TrangThai as TrangThaiHienTai,
                           CASE WHEN c.SoBan IS NOT NULL THEN 1 ELSE 0 END AS DaDat,
                           c.MaDatBan
                    FROM dbo.BAN b
                    LEFT JOIN dbo.CHITIETDATBAN c ON b.SoBan = c.SoBan
                        AND c.NgayDat = @Ngay
                        AND c.GioDat = @Gio
                        AND c.TrangThai = N'Đã đặt'
                    ORDER BY b.SoBan";

                using (var cmd = new SqlCommand(sql, sqlCon))
                {
                    cmd.Parameters.AddWithValue("@Ngay", NgayDat.Date);
                    cmd.Parameters.AddWithValue("@Gio", GioDat);

                    using (var r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            int soBan = r.GetInt16(0);
                            string trangThaiHienTai = r.IsDBNull(1) ? "Trống" : r.GetString(1); // "Có thể sử dụng" hoặc "Đang sử dụng"
                            int daDat = r.IsDBNull(2) ? 0 : r.GetInt32(2);
                            int maDatBan = r.IsDBNull(3) ? 0 : Convert.ToInt32(r.GetValue(3));

                            string trangThai;
                            if (trangThaiHienTai == "Đang sử dụng")
                            {
                                trangThai = "Có khách"; // Bàn đang có khách (màu đỏ)
                            }
                            else if (daDat == 1)
                            {
                                trangThai = "Đã đặt"; // Bàn đã được đặt trước cho khung giờ này
                            }
                            else
                            {
                                trangThai = "Trống"; // Bàn trống
                            }

                            DanhSachBan.Add(new BanItem
                            {
                                SoBan = soBan,
                                TrangThai = trangThai,
                                MaDatBan = maDatBan
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi load bàn theo giờ: " + ex.Message);
                // Fallback: load tất cả bàn
                LoadTatCaBan();
            }
            finally { CloseConnect(); }
        }

        private void OpenConnect()
        {
            if (sqlCon == null)
                sqlCon = new SqlConnection(strCon);

            if (sqlCon.State == ConnectionState.Closed) sqlCon.Open();
        }

        private void CloseConnect()
        {
            if (sqlCon?.State == ConnectionState.Open) sqlCon.Close();
        }

        // Hàm cập nhật tình trạng bàn sau khi đặt (đổi màu trong TinhTrangBanViewModel)
        private void CapNhatTinhTrangBanSauKhiDat(int soBan, int maDatBan)
        {
            // Cập nhật trực tiếp lên DanhSachBan nếu đã có item
            var item = DanhSachBan != null ? System.Linq.Enumerable.FirstOrDefault(DanhSachBan, b => b.SoBan == soBan) : null;
            if (item != null)
            {
                item.MaDatBan = maDatBan;
                item.TrangThai = "Đã đặt";
                AppEvents.RaiseBookingUpdated(soBan, "Đã đặt", maDatBan);
                return;
            }

            // Nếu không tìm thấy (ví dụ view đang hiển thị ngày khác) -> reload
            LoadBanTrong();
            AppEvents.RaiseBookingUpdated(soBan, "Đã đặt", maDatBan);
        }
    }

    // Class hỗ trợ hiển thị bàn
    public class BanItem : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private int _soBan;
        public int SoBan
        {
            get => _soBan;
            set
            {
                _soBan = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SoBan)));
            }
        }

        private string _trangThai;
        public string TrangThai
        {
            get => _trangThai;
            set
            {
                _trangThai = value;
                System.Diagnostics.Debug.WriteLine($"Bàn {SoBan}: TrangThai = '{value}'"); // DEBUG
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TrangThai)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HienThi)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(MauChu)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DoDam)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CoTheChon)));
            }
        }

        private int _maDatBan;
        public int MaDatBan
        {
            get => _maDatBan;
            set
            {
                _maDatBan = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(MaDatBan)));
            }
        }

        public string HienThi
        {
            get
            {
                if (TrangThai == "Có khách")
                    return $"Bàn {SoBan} - CÓ KHÁCH";
                else if (TrangThai == "Đã đặt")
                    return $"Bàn {SoBan} - ĐÃ ĐẶT";
                else
                    return $"Bàn {SoBan}";
            }
        }

        public Brush MauChu
        {
            get
            {
                if (TrangThai == "Có khách")
                    return Brushes.Red; // Bàn có khách màu đỏ
                else if (TrangThai == "Đã đặt")
                    return Brushes.Gray; // Bàn đã đặt màu xám
                else
                    return Brushes.White; // Bàn trống màu trắng
            }
        }

        public FontWeight DoDam
        {
            get
            {
                return (TrangThai == "Trống") ? FontWeights.Bold : FontWeights.Normal;
            }
        }

        // Thêm property để binding IsEnabled
        public bool CoTheChon
        {
            get
            {
                return TrangThai == "Trống";
            }
        }
    }
}
