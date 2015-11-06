using System;
using System.Linq.Expressions;

namespace Elephanet.Expressions
{
    public class JsonbExpression  : Expression
    {
    }

    public class JsonbTableExpression: JsonbExpression
    {
        readonly string _name;
        public JsonbTableExpression()
        {
            _name = string.Format("{0}_{1}", Type.Namespace, Type.Name);
        }
    }
}
