using System;
using System.Data;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using MySql.Data.MySqlClient;
using MySql;
using _min.Models;

namespace _min
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            
            InitializeComponent();

        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            string connstring = "Server=109.74.158.75;Uid=dotnet;Pwd=dotnet;Database=ks;pooling=false";
            DataTable log = new DataTable();
            log.Columns.Add(new DataColumn("query", typeof(string)));
            log.Columns.Add(new DataColumn("time", typeof(int)));
            BaseDriverMySql driver = new BaseDriverMySql(connstring, log);
            DateTime actDate = DateTime.Now;
            int[] inlist = { 18, 28, 38, 39 };
            DataTable table = driver.fetchAll("SELECT DATEDIFF(DATE(", actDate, "), `date`)", " FROM `users` WHERE `id` IN ", inlist);
            
            label1.Content = table.Rows.Count.ToString();

            Field f1 = new Field(1, "col", 1, "type", 1);
            //label1.Content += f1.column + " " + f1.typeName;
            //f1.typeName = "dddd";

            DataTable tree = new DataTable("tree");
            tree.Columns.Add("id", typeof(int));
            tree.Columns.Add("parent", typeof(int));

            tree.Rows.Add(1, null);
            tree.Rows.Add(2, 1);
            tree.Rows.Add(3, 1);
            tree.Rows.Add(4, 2);
            DataSet ds = new DataSet();
            ds.Tables.Add(tree);
            ds.Relations.Add(new DataRelation("r", tree.Columns[0], tree.Columns[1]));
            DataRow[] children = ds.Tables["tree"].Rows[0].GetChildRows("r");
            foreach (DataRow r in children) {
                label1.Content += " ch " + r[0]; 
            }

            
            
            MySqlConnection conn = new MySqlConnection("Server=109.74.158.75;Uid=dotnet;Pwd=dotnet;Database=ks;pooling=false");
            MySqlCommand cmd = new MySqlCommand("SELECT * FROM users LIMIT 1", conn);
            
            

            conn.Open();
            MySqlDataAdapter adap = new MySqlDataAdapter("SELECT * FROM users LIMIT 1", conn);
            label1.Content = conn.State.ToString();
            DataTable tab = new DataTable();
            adap.Fill(tab);
            textBox2.VerticalScrollBarVisibility = ScrollBarVisibility.Visible;
            foreach (DataColumn col in tab.Columns) {
                textBox2.Text += col.ColumnName + Environment.NewLine;
                foreach (var prop in col.GetType().GetProperties())
                {
                    textBox2.Text +=  prop.Name + " = " + prop.GetValue(col, null) + Environment.NewLine;
                }
            }


            
            /*
            DataRow row = tab.Rows[0];
            foreach(DataColumn col in row.Table.Columns){
                label1.Content += Environment.NewLine + col.ColumnName + " "
                + row[col.ColumnName];
            }
            conn.Close();
             */

            
            
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {

        }

    }
}
