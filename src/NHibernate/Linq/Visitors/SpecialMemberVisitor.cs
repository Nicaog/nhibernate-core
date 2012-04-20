using System.Linq.Expressions;
using NHibernate.Engine;
using NHibernate.Linq.Functions;
using Remotion.Linq.Utilities;

namespace NHibernate.Linq.Visitors
{
    public interface IMethodCallVisitor
    {
        Expression Visit(MethodCallExpression expression);
    }

    public interface IMemberVisitor
    {
        Expression Visit(MemberExpression expression);
    }

    /// <summary>
    /// Deals with methods and properties that may require special attention
    /// </summary>
    public class SpecialMemberVisitor : NhExpressionTreeVisitor
    {
        private readonly ILinqToHqlGeneratorsRegistry _generatorsRegistry;

        public static Expression Visit(Expression expression, ISessionFactory factory)
        {
            var factoryImplementor = (ISessionFactoryImplementor)factory;

            var visitor = new SpecialMemberVisitor(factoryImplementor.Settings.LinqToHqlGeneratorsRegistry);

            return visitor.VisitExpression(expression);
        }

        private SpecialMemberVisitor(ILinqToHqlGeneratorsRegistry generatorsRegistry)
        {
            _generatorsRegistry = generatorsRegistry;
        }

        protected override Expression VisitUnaryExpression(UnaryExpression expression)
        {
            ArgumentUtility.CheckNotNull("expression", expression);
            Expression newOperand = VisitExpression(expression.Operand);
            if (newOperand != expression.Operand)
            {
                //no need to convert
                if (expression.Type == newOperand.Type)
                    return newOperand;
                
                if (expression.NodeType == ExpressionType.UnaryPlus)
                    return Expression.UnaryPlus(newOperand, expression.Method);
                
                return Expression.MakeUnary(expression.NodeType, newOperand, expression.Type, expression.Method);
            }
            else
                return expression;
        }

        protected override Expression VisitMethodCallExpression(MethodCallExpression expression)
        {
            //check if there is a custom handler
            IHqlGeneratorForMethod generator;
            if(_generatorsRegistry.TryGetGenerator(expression.Method, out generator))
            {
                var visitor = generator as IMethodCallVisitor;
                if(visitor != null)
                    return visitor.Visit(expression);
            }

            return base.VisitMethodCallExpression(expression);
        }

        protected override Expression VisitMemberExpression(MemberExpression expression)
        {
            //check if there is a custom handler
            IHqlGeneratorForProperty generator;
            if(_generatorsRegistry.TryGetGenerator(expression.Member, out generator))
            {
                var visitor = generator as IMemberVisitor;
                if(visitor != null)
                    return visitor.Visit(expression);
            }

            return base.VisitMemberExpression(expression);
        }
    }
}