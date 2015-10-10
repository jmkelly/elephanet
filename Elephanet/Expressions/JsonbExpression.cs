using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Elephanet.Expressions
{
    public class JsonbExpression  : Expression
    {
        public JsonbExpression()
            : base()
        {

        }
    }

    public class JsonbTableExpression: JsonbExpression
    {
        private string _name;
        public JsonbTableExpression()
        {
            _name = string.Format("{0}_{1}", Type.Namespace, Type.Name);
        }
    }
}
