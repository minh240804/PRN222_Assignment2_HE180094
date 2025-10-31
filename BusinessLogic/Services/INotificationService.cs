using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLogic.Services
{
    public interface INotificationService
    {
        Task SendTagListUpdate();
        Task SendToast(string message);
        // Thêm các phương thức khác nếu cần (ví dụ: SendNewArticleNotification, ...)
    }
}
