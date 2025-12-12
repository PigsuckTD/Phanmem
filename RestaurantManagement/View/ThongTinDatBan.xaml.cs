using System;
using System.Windows;

namespace QuanLyNhaHang.View
{
    public partial class ThongTinDatBanWindow : Window
    {
        public string ActionResult { get; private set; } = "NONE";

        public ThongTinDatBanWindow(ThongTinDatBanData data)
        {
            InitializeComponent();
            DataContext = data;
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            ActionResult = "NONE";
            this.Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Bạn có chắc chắn muốn HỦY đặt bàn này?",
                "Xác nhận hủy",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                ActionResult = "HUY";
                this.Close();
            }
        }

        private void BtnKhachDen_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Xác nhận khách đã đến và chuyển bàn sang trạng thái 'Đang sử dụng'?",
                "Xác nhận khách đến",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                ActionResult = "KHACH_DEN";
                this.Close();
            }
        }
    }

    public class ThongTinDatBanData
    {
        public int MaDatBan { get; set; }
        public int SoBan { get; set; }
        public string TenKhach { get; set; }
        public string SDT { get; set; }
        public DateTime NgayDat { get; set; }
        public string NgayDatDisplay => NgayDat.ToString("dd/MM/yyyy");
        public string GioDat { get; set; }
        public int SoNguoi { get; set; }
        public string UuDai { get; set; }
        public string GhiChu { get; set; }

        public Visibility HasUuDai => string.IsNullOrWhiteSpace(UuDai) ? Visibility.Collapsed : Visibility.Visible;
        public Visibility HasGhiChu => string.IsNullOrWhiteSpace(GhiChu) ? Visibility.Collapsed : Visibility.Visible;

        public string ThoiGianConLai
        {
            get
            {
                try
                {
                    var thoiGianDat = NgayDat.Date.Add(TimeSpan.Parse(GioDat));
                    var thoiGianHetHan = thoiGianDat.AddMinutes(30);
                    var conLai = thoiGianHetHan - DateTime.Now;

                    if (conLai.TotalMinutes > 0)
                    {
                        return $"⏰ Còn {(int)conLai.TotalMinutes} phút trước khi tự động hủy";
                    }
                    else
                    {
                        return "⚠️ Đã quá thời gian đặt bàn!";
                    }
                }
                catch
                {
                    return "";
                }
            }
        }
    }
}