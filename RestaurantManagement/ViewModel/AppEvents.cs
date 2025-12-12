using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Phanmem.ViewModel
{
    /// <summary>
    /// Hub sự kiện để DatBanViewModel phát tín hiệu cho TinhTrangBanViewModel
    /// </summary>
    public static class AppEvents
    {
        /// <param name="int">SoBan</param>
        /// <param name="string">TrangThaiMoi</param>
        /// <param name="int">MaDatBan (có thể = 0)</param>
        public static event Action<int, string, int> BookingUpdated;

        public static void RaiseBookingUpdated(int soBan, string trangThaiMoi, int maDatBan = 0)
        {
            BookingUpdated?.Invoke(soBan, trangThaiMoi, maDatBan);
        }
    }
}
