using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using _min.Interfaces;
using MySql.Data.MySqlClient;
using MySql;
using System.Data;
using System.Collections;
using System.Diagnostics;

namespace _min.Models
{

    class BaseDriverMySql : IBaseDriver
    {
        private MySqlConnection conn;
        protected DataTable logTable; // null alebo string query, int time

        private enum QueryType 
        {
            Unknown, Insert, Update, Delete, Select 
        };
        private enum Expectations
        {
            Single, InsertValues, UpdateValues, Conditions, InList, Columns
        };

        private static readonly Dictionary<string, Expectations> lastWordsDecoder =
                new Dictionary<string, Expectations>(StringComparer.OrdinalIgnoreCase)
        {
            {"IN", Expectations.InList},
            {"INSERT", Expectations.InsertValues},
            {"INTO", Expectations.InsertValues},
            {"SET", Expectations.UpdateValues},
            {"WHERE", Expectations.Conditions},            
            {"HAVING", Expectations.Conditions},
            {"SELECT", Expectations.Columns}
        };


        public BaseDriverMySql(string connstring, DataTable logTable = null)
        {
            conn = new MySqlConnection(connstring);
            this.logTable = logTable;
        }

        public static string escape(object o) {
            if (o == null) return " NULL ";
            if (o is DateTime) return String.Format("'{0:yyyy-MM-dd HH:mm:ss}'", (DateTime)o);
            //if (o is double || o is long) return o.ToString();
            double parsed;
            if(double.TryParse(o.ToString(), out parsed)) return parsed.ToString();
            return "\"" + MySqlHelper.EscapeString(o.ToString()) + "\"";
        }

        private QueryType getQueryType(string query) {
            string firstWord = query.Split(' ').First();
            switch (firstWord.ToUpper())
            {
                case "SELECT":
                case "SHOW":
                    return QueryType.Select;
                case "INSERT":
                    return QueryType.Insert;
                case "UPDATE":
                    return QueryType.Update;
                case "DELETE":
                    return QueryType.Delete;
                default:
                    throw new FormatException("Unrecognised type of MySql Command");
            }
        }

        private void log(string query, Stopwatch watch){
                DataRow logInfo = logTable.NewRow();
                logInfo["query"] = query;
                logInfo["time"] = watch.ElapsedMilliseconds;
                logTable.Rows.Add(logInfo);
        }

        private Dictionary<string, object> DataRowToDictionary(DataRow row) {
            Dictionary<string, object> res = new Dictionary<string, object>();
            foreach (DataColumn col in row.Table.Columns)
            {
                res[col.ColumnName] = row[col.ColumnName];
            }
            return res; 
        }
        

        /* translate dibi-style query to string */
        protected string translate(params object[] parts) {

            // expects single value, other expectations are array-wise
            Expectations expect = Expectations.Single;
            StringBuilder resultQuery = new StringBuilder();

            foreach(object part in parts){
                if(part is string){ // strings are directly appended
                    string pString = (string)part;
                    resultQuery.Append(pString);
                    string[] words = pString.Split(' ');
                    foreach (string word in words)
                    {
                        if (lastWordsDecoder.ContainsKey(word))
                        {
                            expect = lastWordsDecoder[word];
                        }
                    }
                }
                else
                    if(part is int || part is long || part is float || part is double){
                        resultQuery.Append(" " + part + " ");
                    }
                else{
                    bool handled = false;
                    object dictionarizedPart = part;
                    Dictionary<string, object> dict;
                    switch (expect) { 
                        case Expectations.InsertValues:
                            if (part is DataRow)
                            {
                                dictionarizedPart = DataRowToDictionary(part as DataRow);    
                            }
                            if (dictionarizedPart is Dictionary<string, object>) {
                                resultQuery.Append(" ( ");
                                dict = (Dictionary<string, object>)dictionarizedPart;
                                bool first = true;
                                StringBuilder valuesPart = new StringBuilder();
                                foreach (string key in dict.Keys)
                                {
                                    resultQuery.Append((first ? "" : ", ") + "`" + key + "`");
                                    valuesPart.Append((first ? "" : ", ") + escape(dict[key]));
                                    first = false;
                                }
                                resultQuery.Append(") VALUES ( ");
                                resultQuery.Append(valuesPart.ToString());
                                resultQuery.Append(")");
                                handled = true;
                            }    
                            break;
                        case Expectations.UpdateValues:
                            if (part is DataRow)
                            {
                                dictionarizedPart = DataRowToDictionary(part as DataRow);    
                            }
                            if (dictionarizedPart is Dictionary<string, object>)
                            {
                                dict = (Dictionary<string, object>)dictionarizedPart;
                                bool first = true;
                                foreach (string col in dict.Keys)
                                {
                                    resultQuery.Append((first ? "" : ", ") +
                                        "`" + col + "` = " +
                                        escape(dict[col]));
                                    first = false;
                                }
                                handled = true;
                            }
                            break;
                        case Expectations.InList:
                            if (part is IEnumerable)
                            {
                                resultQuery.Append("(");
                                bool first = true;
                                foreach(object item in (part as IEnumerable))
                                {
                                    resultQuery.Append((first ? "" : ", ") +
                                        escape(item));
                                    first = false;
                                }
                                resultQuery.Append(") ");
                                handled = true;
                            }
                            break;
                        case Expectations.Conditions:
                            if (part is ConditionMySql)
                            {
                                resultQuery.Append(((ConditionMySql)part).Translate());
                                handled = true;
                            }
                            break;
                        case Expectations.Single:
                        case Expectations.Columns:
                            if (part is IEnumerable)    // column
                            {
                                bool first = true;
                                foreach (object item in (part as IEnumerable))
                                {
                                    resultQuery.Append(first ? "" : ", ");
                                    resultQuery.Append("`" + (item as string) + "`");
                                    first = false;
                                }
                                handled = true;
                            }
                            else {  // single
                                if (part is DateTime)
                                {
                                    DateTime timePart = (DateTime)part;
                                    string formatted = String.Format("'{0:yyyy-MM-dd HH:mm:ss}'", timePart);
                                    resultQuery.Append(formatted);
                                    handled = true;
                                }
                                if (part is int || part is double)
                                {
                                    resultQuery.Append(part.ToString());
                                    handled = true;
                                }
                            }
                            break;
                    }
                    if (!handled) throw new FormatException("Unrecognized non-string part in query");
                    // because it is not a string nor suits the Expectations
                }
            }
            
            return resultQuery.ToString();
        }

        public System.Data.DataTable fetchAll(params object[] parts)
        {
            string query = this.translate(parts);
            QueryType type = this.getQueryType(query);
            if (type != QueryType.Select)
            {
                throw new Exception("Trying to fetch from a non-select query");
            }
            DataTable result = new DataTable();
            MySqlDataAdapter adapter = new MySqlDataAdapter(query, conn);
            Stopwatch watch = new Stopwatch();
            if (logTable is DataTable) {
                watch.Start();
            }
            try
            {
                conn.Open();
                adapter.Fill(result);
            }
            finally {
                conn.Close();
            }
            if (logTable is DataTable) {
                watch.Stop();
                this.log(query, watch);
            }
            return result;
        }

        public System.Data.DataRow fetch(params object[] parts)
        {
            DataTable table = this.fetchAll(parts);
            if (table.Rows.Count > 0)
                return table.Rows[0];
            else
                return null;
        }

        public object fetchSingle(params object[] parts)
        {
            DataTable table = this.fetchAll(parts);
            if (table.Rows.Count > 0)
                return table.Rows[0][0];
            else
                return null;
        }

        public int query(params object[] parts)
        {
            string query = this.translate(parts);
            int rowsAffected = 0;

            MySqlCommand cmd = new MySqlCommand(query, conn);
            Stopwatch watch = new Stopwatch();
            if (logTable is DataTable)
            {
                watch.Start();
            }
            try
            {
                conn.Open();
                rowsAffected = cmd.ExecuteNonQuery();
            }
            finally {
                conn.Close();
            }
            if (logTable is DataTable)
            {
                watch.Stop();
                this.log(query, watch);
            }
            return rowsAffected;
        }

        public void StartTransaction() {
            query("START TRANSACTION");
        }

        public void CommitTransaction()
        {
            query("COMMIT");
        }
        public void RollbackTransaction()
        {
            query("ROLLBACK");
        }

        public int NextId(string tableName) {
            DataRow status = fetch("SHOW TABLE STATUS LIKE '" + tableName + "'");
            return Convert.ToInt32(status["Auto_increment"]);
        }

    }
}
