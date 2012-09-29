using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using _min.Interfaces;


namespace _min.Models
{
    class ConditionMySql : ICondition
    {
            private DataRow lowerBounds;
            private DataRow upperBounds;    // optional
            
            public ConditionMySql(DataRow lBounds, DataRow uBounds = null)
            {
                lowerBounds = lBounds;
                upperBounds = uBounds;
            }

            public string Translate()
            {
                StringBuilder result = new StringBuilder();
                bool first = true;

                if(upperBounds == null){
                    foreach(DataColumn col in lowerBounds.Table.Columns){
                        result.Append(first?"":" AND ");
                        result.Append(" `" + col.ColumnName + "` = " 
                            + BaseDriverMySql.escape(lowerBounds[col.ColumnName]));
                    }
                }
                else{
                    foreach(DataColumn col in lowerBounds.Table.Columns){
                        result.Append(first?"":" AND `" + col.ColumnName + "` ");
                        if(col.DataType == typeof(int) || col.DataType == typeof(double)){
                            result.Append("BETWEEN " + lowerBounds[col.ColumnName] 
                                + " AND " + upperBounds[col.ColumnName]);
                        }
                        else{
                            result.Append(" <= " + BaseDriverMySql.escape(lowerBounds[col.ColumnName]) 
                                + " AND `" + col.ColumnName + "` >= "
                                + BaseDriverMySql.escape(upperBounds[col.ColumnName]));
                        }
                    }                
                }

                return result.ToString();
            }
        }
    }
