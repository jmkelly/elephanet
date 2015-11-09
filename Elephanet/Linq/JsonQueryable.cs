using Elephanet.Linq;
using Remotion.Linq;
using Remotion.Linq.Parsing.Structure;
using System.Linq;
using System.Linq.Expressions;

namespace Elephanet.Linq
{

    public interface IJsonQueryable<T> : IQueryable<T>
    { }

       

    public class JsonQueryable<T> : QueryableBase<T>, IJsonbQueryable<T>
    {
        public JsonQueryable(IQueryParser queryParser, IJsonQueryExecutor executor) : base(queryParser, executor)
        {

        }

        public JsonQueryable(IQueryProvider provider) : base(provider)
        {

        }

        public JsonQueryable(IQueryProvider provider, Expression expression) : base(provider, expression)
        {

        }

    }
}
