using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace StockNotification
{
    public partial class MainWindow : Window
    {
        private readonly string connectionString = "Data Source=RONI;Database=InventorySystemdatabaseUpdated;Integrated Security=True;Encrypt=True;TrustServerCertificate=True";
        private readonly List<StockItem> stockList = new List<StockItem>(); // Caching for filtering

        public MainWindow()
        {
            InitializeComponent();
            LoadStockData();
        }

        private void LoadStockData()
        {
            stockList.Clear();

            using SqlConnection conn = new(connectionString);
            try
            {
                conn.Open();
                string query = @"
                SELECT a.Item_Name, a.Item_Quantity, a.Item_ID, a.Item_Description,
                    CASE 
                        WHEN d.Item_ID IS NOT NULL THEN 'Damaged'
                        WHEN b.Item_ID IS NOT NULL THEN 'Borrowed'
                        WHEN a.Item_Quantity = 0 THEN 'No Stock'
                        WHEN a.Item_Quantity <= a.Item_Low_Indicator THEN 'Low Stock'
                        ELSE 'Available'
                    END AS Status
                FROM Roni.InventorySystemdatabaseUpdated.dbo.AvailableItems a
                LEFT JOIN Roni.InventorySystemdatabaseUpdated.dbo.BorrowedItems b ON a.Item_ID = b.Item_ID
                LEFT JOIN Roni.InventorySystemdatabaseUpdated.dbo.ReportedItems d ON a.Item_ID = d.Item_ID";

                SqlCommand cmd = new(query, conn);
                SqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    stockList.Add(new StockItem
                    {
                        EquipmentID = reader["Item_ID"]?.ToString() ?? string.Empty,
                        Name = reader["Item_Name"]?.ToString() ?? string.Empty,
                        Stock = reader["Item_Quantity"] != DBNull.Value ? Convert.ToInt32(reader["Item_Quantity"]) : 0, // Handle null
                        Description = reader["Item_Description"]?.ToString() ?? string.Empty,
                        Status = reader["Status"]?.ToString() ?? string.Empty
                    });
                }

                reader.Close();
                StockNotificationDataGrid.ItemsSource = stockList;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading stock data: {ex.Message}", "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // **Filter Methods for Each Status**
        private void Button_Damaged_Click(object sender, RoutedEventArgs e)
        {
            FilterData("Damaged");
        }

        private void Button_Borrowed_Click(object sender, RoutedEventArgs e)
        {
            FilterData("Borrowed");
        }

        private void Button_LowStock_Click(object sender, RoutedEventArgs e)
        {
            FilterData("Low Stock");
        }

        private void Button_NoStock_Click(object sender, RoutedEventArgs e)
        {
            FilterData("No Stock");
        }

        private void Button_Reset_Click(object sender, RoutedEventArgs e)
        {
            StockNotificationDataGrid.ItemsSource = stockList; // Reload all items
            StockNotificationDataGrid.Items.Refresh(); // Ensure UI updates
        }

        // **Filter Function**
        private void FilterData(string status)
        {
            var filteredItems = stockList.FindAll(item => item.Status == status);
            StockNotificationDataGrid.ItemsSource = filteredItems;
            StockNotificationDataGrid.Items.Refresh(); // Ensure UI updates
        }

        // **Highlighting Based on Status**
        private void StockNotificationDataGrid_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            if (e.Row.Item is StockItem stockItem)
            {
                e.Row.Background = stockItem.Status switch
                {
                    "Borrowed" => new SolidColorBrush(Colors.LightBlue),
                    "Low Stock" => new SolidColorBrush(Colors.Yellow),
                    "No Stock" => new SolidColorBrush(Colors.Red),
                    "Damaged" => new SolidColorBrush(Colors.Gray),
                    _ => e.Row.Background
                };
            }
        }

        private void StockNotificationDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (StockNotificationDataGrid.SelectedItem is StockItem selectedItem)
            {
                MessageBox.Show($"Selected Item: {selectedItem.Name}", "Selection Changed");
            }
        }
    }

    public class StockItem
    {
        public string EquipmentID { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int Stock { get; set; }
        public string Description { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }
}
