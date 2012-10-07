using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace _min.Common
{
    public static class Functions
    {
        /// <summary>
        /// Tries to convert string to given type if TryParse method exists in the type
        /// </summary>
        /// <param name="str"></param>
        /// <param name="t"></param>
        /// <param name="res"></param>
        /// <returns>TryParse was present && string was successfully parsed</returns>
        public static bool TryTryParse(string str, Type t, out object res) 
        {
            var parseMethod = t.GetMethod("TryParse", new Type[] { typeof(string), t.MakeByRefType() });
            if (parseMethod != null)
            {
                object objectArgument = null;
                object[] tryParseParams = new object[] { str, objectArgument };
                if ((bool)parseMethod.Invoke(t, tryParseParams))
                {
                    res = Convert.ChangeType(tryParseParams[1], t);
                    return true;
                }

            }
            res = null;
            return false;
        }
    }
}
