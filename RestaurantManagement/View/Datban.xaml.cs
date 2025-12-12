using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using QuanLyNhaHang.ViewModel;

namespace QuanLyNhaHang.View
{
    /// <summary>
    /// Interaction logic for Datban.xaml
    /// </summary>
    public partial class DatBan : UserControl
    {
        public DatBan()
        {
            InitializeComponent();
        }

        private void TangSoNguoi_Click(object sender, RoutedEventArgs e)
        {
            var vm = DataContext as DatBanViewModel;
            if (vm != null && vm.SoNguoi < 20) // Giới hạn tối đa 20 người
            {
                vm.SoNguoi++;
            }
        }

        private void GiamSoNguoi_Click(object sender, RoutedEventArgs e)
        {
            var vm = DataContext as DatBanViewModel;
            if (vm != null && vm.SoNguoi > 1) // Tối thiểu 1 người
            {
                vm.SoNguoi--;
            }
        }

        private void BtnHuyDatBan_Click(object sender, RoutedEventArgs e)
        {
            // Hiển thị hộp thoại xác nhận
            var result = MessageBox.Show(
                "Bạn có chắc chắn muốn hủy đặt bàn?\nToàn bộ thông tin đã nhập sẽ bị xóa.",
                "Xác nhận hủy",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question
            );

            // Nếu người dùng chọn Yes
            if (result == MessageBoxResult.Yes)
            {
                var vm = DataContext as DatBanViewModel;
                if (vm != null)
                {
                    // Reset toàn bộ form
                    vm.TenKhach = "";
                    vm.SDT = "";
                    vm.BanDuocChon = null;
                    vm.SoNguoi = 2;
                    vm.NgayDat = DateTime.Today;
                    vm.GioDat = null;
                    vm.UuDai = vm.UuDaiList.FirstOrDefault();
                    vm.GhiChu = "";
                }

                MessageBox.Show(
                    "Đã hủy đặt bàn thành công!",
                    "Thông báo",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );
            }
            // Nếu chọn No thì không làm gì cả
        }
    }
}