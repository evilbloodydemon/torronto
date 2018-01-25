using LinqToDB.SqlQuery;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;

namespace Torronto.Lib.Sphinx
{
    public enum Sorting
    {
        Ascending,
        Descending
    }

    public class WhereStatement
    {
        private readonly List<WhereClause> _whereClauses = new List<WhereClause>();

        public int Count
        {
            get { return _whereClauses.Count; }
        }

        public void Add(WhereClause whereClause)
        {
            _whereClauses.Add(whereClause);
        }

        public string Build()
        {
            return string.Join(" AND ", _whereClauses);
        }
    };

    public class WhereClause
    {
        public string Field { get; set; }
        public object CompareValue { get; set; }

        public WhereClause(string field, object compareValue)
        {
            Field = field;
            CompareValue = compareValue;
        }

        public override string ToString()
        {
            return string.Format("`{0}` = {1}", Field, CompareValue);
        }
    }

    public class MatchClause : WhereClause
    {
        public MatchClause(string match)
            : base("MATCH", match)
        {
        }

        public override string ToString()
        {
            var escapedMatch = MySqlHelper.EscapeString(CompareValue.ToString());

            return string.Format("{0}('{1}')", Field, escapedMatch);
        }
    };

    public class SphinxQueryBuilder
    {
        private readonly List<string> _selectedColumns = new List<string>();
        private readonly List<string> _selectedLiterals = new List<string>();
        private readonly List<string> _selectedTables = new List<string>();
        private WhereStatement _whereStatement = new WhereStatement();
        private Tuple<int, int> _limit;

        public SphinxQueryBuilder(params string[] tables)
        {
            SelectFromTables(tables);
        }

        public SphinxQueryBuilder SelectAllColumns()
        {
            _selectedColumns.Clear();

            return this;
        }

        public SphinxQueryBuilder SelectColumns(params string[] columns)
        {
            _selectedColumns.Clear();
            _selectedColumns.AddRange(columns);

            return this;
        }

        public SphinxQueryBuilder SelectLiteral(string literal)
        {
            _selectedLiterals.Add(literal);

            return this;
        }

        private SphinxQueryBuilder SelectFromTables(params string[] tables)
        {
            _selectedTables.Clear();
            _selectedTables.AddRange(tables);

            return this;
        }

        public SphinxQueryBuilder AddWhere(string field, object compareValue)
        {
            _whereStatement.Add(new WhereClause(field, compareValue));

            return this;
        }

        public SphinxQueryBuilder AddMatch(string match)
        {
            _whereStatement.Add(new MatchClause(match));

            return this;
        }

        public SphinxQueryBuilder AddLimits(int offset, int limit)
        {
            _limit = new Tuple<int, int>(offset, limit);

            return this;
        }

        public string Build()
        {
            var query = new StringBuilder();
            query.Append("SELECT ");

            if (_selectedColumns.Count == 0)
            {
                query.Append("*");
            }
            else
            {
                query.AppendFormat("`{0}`", string.Join("`, `", _selectedColumns));
            }

            if (_selectedLiterals.Count > 0)
            {
                query.AppendFormat(", {0}", string.Join(", ", _selectedLiterals));
            }

            query.AppendLine();
            query.Append("FROM ");
            query.AppendFormat("`{0}`", string.Join("`, `", _selectedTables));

            if (_whereStatement.Count > 0)
            {
                query.AppendLine();
                query.Append("WHERE " + _whereStatement.Build());
            }

            if (_limit != null)
            {
                query.AppendLine();
                query.AppendFormat("LIMIT {0}, {1}", _limit.Item1, _limit.Item2);
            }

            return query.ToString();
        }
    }
}