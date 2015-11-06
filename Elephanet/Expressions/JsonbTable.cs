using System;
using System.Linq.Expressions;

namespace Elephanet
{
    public class JsonbTable 
    {
        readonly string _name;
        public JsonbTable(Type type, Expression expression)
        {
            _name = string.Format("@0_@1", type.Namespace, type.Name);
        }

        public string Name { get { return _name; } }
    }

    public class JsonbPath 
    {
        readonly string _name;
        public JsonbPath(string name, Expression expression)
        {
            _name = name;
        }

        public string Name { get { return _name; } }

    }

    public class JsonbValue 
    {
        readonly string _value;
        readonly Expression _expression;
        public JsonbValue(string value, Expression expression)
        {
            _value = value;
            _expression = expression;
        }
    }
}
