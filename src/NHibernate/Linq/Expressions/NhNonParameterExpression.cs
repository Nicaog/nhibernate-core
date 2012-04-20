using System.Linq.Expressions;
using NHibernate.Linq.Visitors;
using Remotion.Linq.Clauses.Expressions;
using Remotion.Linq.Parsing;

namespace NHibernate.Linq.Expressions
{
    /// <summary>
    /// Wraps constant expressions so that they are not parameterized
    /// <para>Necessary in cases where the constant refers to a column and not value</para>
    /// </summary>
    public class NhNonParameterExpression : ExtensionExpression
    {
        private readonly ConstantExpression _constantExpression;

        public NhNonParameterExpression(ConstantExpression constantExpression)
            : base(constantExpression.Type)
        {
            _constantExpression = constantExpression;
        }

        public ConstantExpression Inner
        {
            get { return _constantExpression; }
        }

        protected override Expression VisitChildren(ExpressionTreeVisitor visitor)
        {
            //only the key maker is allowed to visit
            if (visitor is ExpressionKeyVisitor)
            {
                var result = visitor.VisitExpression(_constantExpression);
                if (result != _constantExpression)
                {
                    if(result is ConstantExpression)
                        return new NhNonParameterExpression((ConstantExpression)result);

                    return result;
                }
                    
            }

            //hide the child from visitation
            return this;
        }

        public override string ToString()
        {
            return _constantExpression.ToString();
        }

        public static explicit operator ConstantExpression(NhNonParameterExpression expression)
        {
            return expression.Inner;
        }
    }
}