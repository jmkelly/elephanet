using System.Linq.Expressions;

namespace Elephanet.Expressions
{
    public class SqlExpression : Expression
    {
        readonly Sql _query;
 
        public SqlExpression(Sql query) {
            _query = query; 
        }

        public Sql Query { get { return _query; } }

    }
}
