using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MySql.Data.MySqlClient;

namespace Torronto.Lib.Sphinx
{
    public class SphinxReplaceBuilder
    {
        private readonly string _table;
        private List<Tuple<string, object>> _fields = new List<Tuple<string, object>>();
 
        public SphinxReplaceBuilder(string table)
        {
            _table = table;
        }

        public SphinxReplaceBuilder AddField(string field, object value)
        {
            _fields.Add(new Tuple<string, object>(field, value));

            return this;
        }

        public string Build()
        {
            var fields = _fields.Select(x => x.Item1);
            var values = _fields
                .Select(s => s.Item2 ?? string.Empty)
                .Select(s => MySqlHelper.EscapeString(s.ToString()));

            var sb = new StringBuilder()
                .AppendFormat("REPLACE INTO `{0}`", _table)
                .AppendLine()
                .AppendFormat("(`{0}`)", string.Join("`, `", fields))
                .AppendLine()
                .AppendLine("VALUES")
                .AppendFormat("('{0}')", string.Join("', '", values));

            return sb.ToString();
        }
    }
}