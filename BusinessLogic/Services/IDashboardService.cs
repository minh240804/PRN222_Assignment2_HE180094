using System.Collections.Generic;

namespace Assignment2.BusinessLogic
{
    public interface IDashboardService
    {
        /// <summary>
        /// Get dashboard statistics for admin
        /// </summary>
        DashboardStats GetDashboardStats();
    }

    public class DashboardStats
    {
        public int TotalArticles { get; set; }
        public int TotalAccounts { get; set; }
        public int TotalCategories { get; set; }
        public int TotalComments { get; set; }
        public int PublishedArticles { get; set; }
        public int DraftArticles { get; set; }
        public int ActiveAccounts { get; set; }
        public int InactiveAccounts { get; set; }
    }
}
