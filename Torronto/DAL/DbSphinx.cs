using LinqToDB.Data;

namespace Torronto.DAL
{
    public class DbSphinx : DataConnection
    {
        public DbSphinx()
            : base("sphinx")
        {
        }

    }
}