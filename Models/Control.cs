using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using _min.Interfaces;
using _min.Common;

namespace _min.Models
{
    class Control : IControl
    {
        
        public DataTable data { get; set; }
        public List<string> PKColNames {get; private set;}
        public Common.UserAction action {get; private set;}

        public Control(DataTable data, List<string> PKColNames, UserAction action) {
            this.data = data;
            this.PKColNames = PKColNames;
            this.action = action;
        }

        public Control(DataTable data, string PKColName, UserAction action)
            : this(data, new List<string>(new string[] { PKColName }), action)
        { }

        public Control(string caption, UserAction action)
            : this(new DataTable(), "caption", action)
        {
            data.Columns.Add("caption");
            data.Rows.Add(caption);
        }

    }



    class TreeControl : Control
    {
        public string parentColName { get; private set; }
        public string displayColName { get; private set; }
        public DataSet ds { get; private set; }

        public TreeControl(DataTable data, string PKColName,    // tree controls must have a single-column primary key
            string parentColName, string displayColName,
            UserAction action)
                : base(data, PKColName, action) 
        {
            this.parentColName = parentColName;
            this.displayColName = displayColName;
            ds = new DataSet();
            this.data.TableName = "data";
            ds.Tables.Add(this.data);
            ds.Relations.Add("hierarchy", 
                ds.Tables[0].Columns[PKColNames[0]], ds.Tables[0].Columns[parentColName]);
        }
    }
}
