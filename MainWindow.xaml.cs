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
using System.Data.SqlClient;

namespace TableParser
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MSSQL_DB MSSQL_DB;
        private static string BDName_Field;
        private static string ServerName_Field;
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (serverNameTextBox.Text != "" && baseNameTextBox.Text != "")
            {
                BDName_Field = baseNameTextBox.Text;
                ServerName_Field = serverNameTextBox.Text;
                MSSQL_DB = new MSSQL_DB(BDName_Field, ServerName_Field);
                MSSQL_DB.OpenConnection();
            }
            else
                MessageBox.Show("Enter values");
        }
    }
}
