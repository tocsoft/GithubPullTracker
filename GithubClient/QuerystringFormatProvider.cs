using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GithubClient
{
    class QuerystringFormatProvider : IFormatProvider, ICustomFormatter
    {
        public string Format(string format, object arg, IFormatProvider formatProvider)
        {
            
            string value = "";
            bool encodeOutput = true;
            if (arg != null)
            {
                if (format == null)
                {
                    value = String.Format("{0}", arg);
                }
                else
                {
                    if(format == "raw")
                    {
                        value = arg.ToString();
                        encodeOutput = false;
                    }
                    else if (arg is IFormattable)
                    {
                        value = ((IFormattable)arg).ToString(format, formatProvider);
                    }
                    else if (arg is string)
                    {
                        value = String.Format("{0:" + format + "}", arg);
                    }
                    else
                    {
                        value = arg.ToString();
                    }
                }
            }
            value = value ?? "";
            return encodeOutput ? Uri.EscapeUriString(value) : value;
        }

        public object GetFormat(Type service)
        {
            if (service == typeof(ICustomFormatter))
            {
                return this;
            }
            else
            {
                return null;
            }
        }
    }
}
