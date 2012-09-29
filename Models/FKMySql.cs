using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MySql.Data;
using MySql.Data.MySqlClient;
using _min.Models;
using _min.Interfaces;
using System.Data;

namespace _min.Models
{
    class FKMySql : IFK
    {
        public BaseDriverMySql driver { 
            get {
                return driver;
            }
            set {
                if (driver != null) throw new Exception("Driver already set.");
                driver = value;
            }
        }
        public string myTable { get; private set; }
        public string myColumn { get; private set; }
        public string refTable { get; private set; }
        public string refColumn { get; private set; }
        public string displayColumn { get; private set; }
        public Dictionary<string, int> options { get; private set; }


        public FKMySql(//string fk_table, string fk_column, 
            string myTable, string myColumn,
            string refTable, string refColumn,
            string displayColumn) {
                this.myTable = myTable;
                this.myColumn = myColumn;
                this.refTable = refTable;
                this.refColumn = refColumn;
                this.displayColumn = displayColumn;
                //this.driver = driver;
        }

        //these three methods are the only real meaning of this class

        public bool  validateInput(string inputValue)
        {
            return options.ContainsKey(inputValue);
        }

        public int valueForInput(string inputValue) {
            return options[inputValue];
        }

        public string CaptionForValue(int value) {
            if (driver == null) throw new NullReferenceException("No driver assigned");
            return driver.fetchSingle("SELECT `", displayColumn, 
                "` FROM `", refTable, "` WHERE `", refColumn, "` = ", value) as string;
        }

        public void refreshOptions() {  // need to call this to fill FK with data before use
            if (driver == null) throw new NullReferenceException("No driver assigned");
            DataTable tab = driver.fetchAll("SELECT `" + displayColumn + "`.`" + refColumn + "` FROM `" + refTable);
            if ((tab.Columns[0].DataType != typeof(string)) || (tab.Columns[1].DataType != typeof(int)))
            {
                throw new Exception("Unsuitable foreign key for FKMySql");
            }
            options.Clear();
            foreach (DataRow row in tab.Rows)
            {
                options.Add((string)row[0], (int)row[1]);
            }
        }
    }


    class M2NMappingMySql : FKMySql, IM2NMapping
    {
        public string mapTable { get; private set; }
        public string mapRefColumn {get; private set;}
        public string mapMyColumn {get; private set;}
        
        
        public M2NMappingMySql(
            string myTable, string myColumn, string refTable, string refColumn, string mapTable,
            string displayColumn, string mapMyColumn, string mapRefColumn)
                :base(myTable, myColumn, refTable, refColumn, displayColumn){
            this.mapTable = mapTable;
            this.mapMyColumn = mapMyColumn;
            this.mapRefColumn = mapRefColumn;
        }

        public bool validateWholeInput(List<string> inputValues)
        {
            foreach (string iv in inputValues)
                if (!validateInput(iv))
                    return false;
            return true;
        }

        public List<int> valuesForInput(List<string> inputValues) { 
            List<int> res = new List<int>();
            foreach (string iv in inputValues) {
                res.Add(valueForInput(iv));
            }
            return res;
        }

        public List<string> CaptionsForValues(List<int> values) { 
            List<string> res = new List<string>();
            foreach(int val in values) {
                res.Add(CaptionForValue(val));
            }
            return res;
        }

        public void unMap(int key) {        // clears mapping for given key
            if (driver == null) throw new NullReferenceException("No driver assigned");
            driver.query("DELETE FROM `", mapTable, "` WHERE `", mapMyColumn, "` = ", key);
        }

        public void mapVals(int key, int[] vals) {   // maps to given key
            if (driver == null) throw new NullReferenceException("No driver assigned");
            DataTable table = new DataTable();
            table.Columns.Add(mapMyColumn, typeof(int));
            table.Columns.Add(mapRefColumn, typeof(int));
            DataRow row = table.NewRow();
            foreach (int val in vals) {
                row[0] = key;
                row[1] = val;
                driver.query("INSERT INTO `", mapTable, row);
            }
        }
    }


}
