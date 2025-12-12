using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using Phanmem.ViewModel;

namespace Phanmem.ViewModel
{
    public class BanAnViewModel : INotifyPropertyChanged
    {
        public int SoBan { get; set; }

        private string _trangThai;     // "Trong", "DaDat", "DangAn"...
        public string TrangThai
        {
            get => _trangThai;
            set
            {
                _trangThai = value;
                OnPropertyChanged(nameof(TrangThai));
                OnPropertyChanged(nameof(MauNen)); // cho UI update màu
            }
        }

        public Brush MauNen
        {
            get
            {
                return TrangThai switch
                {
                    "Trống" => Brushes.LightGreen,
                    "Đã đặt" => Brushes.Orange,
                    "Có Khách" => Brushes.Red,
                    _ => Brushes.Gray
                };
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

}
