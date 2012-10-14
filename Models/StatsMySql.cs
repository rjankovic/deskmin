using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using _min.Interfaces;
using System.Reflection;

namespace _min.Models
{
    class StatsMySql : BaseDriverMySql, IStats 
    {
        private string webDb;
        public StatsMySql(string webDb, string connstring, DataTable logTable = null)
            :base(connstring, logTable)
        { 
            this.webDb = webDb;
        }

        public DataColumnCollection columnTypes(string tableName)   // extracts information from the COLUMNS table of INFORMATION_SCHEMA
        {
            DataTable tbl = new DataTable();
            DataTable stats = fetchAll("SELECT * FROM COLUMNS WHERE TABLE_SCHEMA = \"" + webDb + "\" AND TABLE_NAME = \"" + tableName + "\" ORDER BY ORDINAL_POSITION");
            foreach (DataRow r in stats.Rows) { 
                DataColumn col = new DataColumn(r["COLUMN_NAME"] as string);        // set ColumnName
                
                col.ExtendedProperties.Add(Common.Constants.FIELD_POSITION, Convert.ToInt32(r["ORDINAL_POSITION"]));
                string typeStr = r["DATA_TYPE"] as string;      // set DataType
                Type representedType = typeof(object);
                if (typeStr.EndsWith("text") || typeStr.EndsWith("char") || typeStr == "enum") { 
                    representedType = typeof(string);
                    col.ExtendedProperties.Add("length", Convert.ToInt32(r["CHARACTER_MAXIMUM_LENGTH"]));
                }
                else switch (typeStr) { 
                    case "timestamp":
                    case "datetime":
                        representedType = typeof(DateTime);
                        break;
                    case "date":
                        representedType = typeof(DateTime);
                        col.ExtendedProperties.Add("DateOnly", true);
                        break;
                    case "int":
                        representedType = typeof(int);
                        break;
                    case "bigint":
                        representedType = typeof(long);
                        break;
                    case "tinyint":
                        representedType = typeof(short);
                        break;
                    case "float":
                        representedType = typeof(float);
                        break;
                    case "double":
                    case "decimal":
                        representedType = typeof(double);
                        break;
                    default:
                        if(typeStr.StartsWith("ënum"))
                        {
                            typeStr = typeStr.Remove(0, "enum('".Length);
                            // and hopefully replace `'` with `"`
                            typeStr = typeStr.Remove(typeStr.Length-1, 1).Replace("\"", "\\\"").Replace("'", "\"");
                            col.DataType = typeof(Enum);
                            col.ExtendedProperties.Add(Common.Constants.COLUMN_ENUM_VALUES, typeStr);
                        }
                        else
                            throw new Exception("Unrecognised column type: " + typeStr);
                        break;
                }
                col.DataType = representedType;
                
                string extra = r["EXTRA"] as string;    // set AutoIncrement
                if (extra == "auto_increment")
                    col.AutoIncrement = true;
                if(!col.AutoIncrement)
                    col.ExtendedProperties.Add(Common.Constants.COLUMN_EDITABLE, true); // TODO add more restrictive rules...

                object colDefault = r["COLUMN_DEFAULT"];      // set DefaultValue
                if(!((colDefault is DBNull) || (colDefault.ToString() == String.Empty))){
                    string colDefaltStr = colDefault as string;
                    if(colDefaltStr == "CURRENT_TIMESTAMP")
                        col.ExtendedProperties.Remove(Common.Constants.COLUMN_EDITABLE);
                    else{
                       object parsed;
                       if(Common.Functions.TryTryParse(colDefaltStr, col.DataType, out parsed)){
                            col.DefaultValue = parsed;
                       }
                    }
                }

                col.AllowDBNull = ((string)r["IS_NULLABLE"]) == "YES";
                if(!(r["CHARACTER_MAXIMUM_LENGTH"] is DBNull)) col.MaxLength = Convert.ToInt32(r["CHARACTER_MAXIMUM_LENGTH"]);

                tbl.Columns.Add(col);
                
            }       // for each row in stats
            return tbl.Columns;
        }

        public List<IFK> foreignKeys(string tableName)
        {
            
            List<IFK> res = new List<IFK>();
            DataTable stats = fetchAll("SELECT * FROM KEY_COLUMN_USAGE WHERE CONSTRAINT_SCHEMA = \"" 
                + webDb + "\" AND TABLE_NAME = \"" + tableName + "\" AND REFERENCED_COLUMN_NAME IS NOT NULL");
            foreach (DataRow r in stats.Rows)
            {
                res.Add(new FKMySql(r["TABLE_NAME"] as string, r["COLUMN_NAME"] as string, r["REFERENCED_TABLE_NAME"] as string, 
                    r["REFERENCED_COLUMN_NAME"] as string, r["REFERENCED_COLUMN_NAME"] as string));
            }
            return res;
        }

        public List<List<string>> indexes(string tableName)
        {
            DataTable stats = fetchAll("SELECT *, GROUP_CONCAT(COLUMN_NAME) AS COLUMNS FROM KEY_COLUMN_USAGE WHERE CONSTRAINT_SCHEMA = \""
                + webDb + "\" AND TABLE_NAME = \"" + tableName + "\" ORDER BY CONSTRAINT_NAME, ORDINAL_POSITION GROUP BY CONSTRAINT_NAME");
            List<List<string>> res = new List<List<string>>();
            foreach (DataRow r in stats.Rows)
            {
                res.Add(new List<string>(((string)r["COLUMNS"]).Split(',')));
            }
            return res;
        }

        public List<string> primaryKeyCols(string tableName)
        {
            DataTable stats = fetchAll("SELECT COLUMN_NAME FROM KEY_COLUMN_USAGE WHERE CONSTRAINT_SCHEMA = \""
                + webDb + "\" AND TABLE_NAME = \"" + tableName + "\" AND CONSTRAINT_NAME = \"PRIMARY\" "
                + " GROUP BY CONSTRAINT_NAME ORDER BY ORDINAL_POSITION");
            return new List<string>(from row in stats.AsEnumerable() select row["COLUMN_NAME"] as string);
        }

        public List<string> TwoColumnTables() {
            DataTable tab = fetchAll("SELECT TABLE_NAME FROM COLUMNS WHERE TABLE_SCHEMA =  '" + webDb  + "' GROUP BY TABLE_NAME " +
                "HAVING COUNT( * ) = 2");
            return new List<string>( from row in tab.AsEnumerable() select row["TABLE_NAME"] as string );
        }

        public List<string> TableList() {
            DataTable tab = fetchAll("SELECT TABLE_NAME FROM TABLES WHERE TABLE_SCHEMA = '" + webDb + "' AND TABLE_TYPE = \"BASE TABLE\" ORDER BY CREATE_TIME");
            return new List<string>(from row in tab.AsEnumerable() select row["TABLE_NAME"] as string);
        }

        public List<IM2NMapping> findMappings()
        {
            List<IM2NMapping> res = new List<IM2NMapping>();
            List<string> twoColTabs = TwoColumnTables();
            List<List<IFK>> FKs = new List<List<IFK>>(from table in twoColTabs select foreignKeys(table));
            foreach (List<IFK> currTabFKs in FKs)
            {
                if (currTabFKs.Count == 2)
                {
                    // cannot say which table will use the mapping field (the one with more data)
                    //  ASK
                    res.Add(new M2NMappingMySql(currTabFKs[0].refTable, currTabFKs[0].refColumn, currTabFKs[1].refTable,
                        currTabFKs[1].refColumn, currTabFKs[0].myTable, currTabFKs[0].refColumn,
                        currTabFKs[0].myColumn, currTabFKs[1].myColumn));
                    
                    res.Add(new M2NMappingMySql(currTabFKs[1].refTable, currTabFKs[1].refColumn, currTabFKs[0].refTable,
                        currTabFKs[0].refColumn, currTabFKs[1].myTable, currTabFKs[1].refColumn,
                        currTabFKs[1].myColumn, currTabFKs[0].myColumn));
                }
            }
            return res;
        }

        public DateTime TableCreation(string tableName) {
            return (DateTime)fetchSingle("SELECT CREATION_TIME FROM TABLES WHERE TABLE_SCHEMA = '",
                webDb, "' AND TABLE_NAME = '", tableName, "'"); 
        }

    }
}
