using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using NHibernate.Hql.Ast;
using NHibernate.Linq;
using NHibernate.Linq.Expressions;
using NHibernate.Linq.Functions;
using NHibernate.Linq.Visitors;
using NUnit.Framework;
using Remotion.Linq;
using Remotion.Linq.Clauses;
using Remotion.Linq.Parsing.Structure.IntermediateModel;

namespace NHibernate.Test.NHSpecificTest.NH2658
{
    public static class ObjectExtensions
    {
        public static T GetProperty<T>(this object o, string propertyName)
        {
            //no implementation for this test
            throw new NotImplementedException();
        }

        [NodeTypeProviderAttribute(typeof(TestExpressionNode))]
        public static IQueryable<T> TestExtension<T>(this IQueryable<T> query, Expression<Func<T, bool>> clause)
        {
            var methodInfo = ((MethodInfo)MethodBase.GetCurrentMethod()).MakeGenericMethod(typeof(T));
            var queryProvider = query.Provider;
            var callExpression = Expression.Call(methodInfo, query.Expression, clause);
            return queryProvider.CreateQuery<T>(callExpression);
        }
    }

    public class TestExpressionNode : MethodCallExpressionNodeBase
    {
        private readonly LambdaExpression _clause;

        public TestExpressionNode(MethodCallExpressionParseInfo parseInfo, LambdaExpression clause)
            : base(parseInfo)
        {
            _clause = clause;
        }

        #region Overrides of MethodCallExpressionNodeBase

        public override Expression Resolve(ParameterExpression inputParameter, Expression expressionToBeResolved, ClauseGenerationContext clauseGenerationContext)
        {
            return Source.Resolve(inputParameter, expressionToBeResolved, clauseGenerationContext);
        }

        protected override QueryModel ApplyNodeSpecificSemantics(QueryModel queryModel, ClauseGenerationContext clauseGenerationContext)
        {
            var whereClause = new WhereClause(_clause.Body);
            queryModel.BodyClauses.Add(whereClause);
            return queryModel;
        }

        #endregion
    }
    
    [TestFixture]
    public class Fixture : TestCase
    {
        public class DynamicPropertyGenerator : BaseHqlGeneratorForMethod, IMethodCallVisitor
        {
            public DynamicPropertyGenerator()
            {
                //just registering for string here, but in a real implementation we'd be doing a runtime generator
                SupportedMethods = new[] { ReflectionHelper.GetMethodDefinition(() => ObjectExtensions.GetProperty<string>(null, null)) };
            }

            public override HqlTreeNode BuildHql(MethodInfo method, Expression targetObject, 
                ReadOnlyCollection<Expression> arguments, HqlTreeBuilder treeBuilder, IHqlExpressionVisitor visitor)
            {
                var propertyName = (string)((NhNonParameterExpression) arguments[1]).Inner.Value;

                return treeBuilder.Dot(
                        visitor.Visit(arguments[0]).AsExpression(),
                        treeBuilder.Ident(propertyName)).AsExpression();
            }

            public Expression Visit(MethodCallExpression expression)
            {
                //wrap the constant that represents the property name or it will be cached
                return Expression.Call(expression.Method, expression.Arguments[0],
                                       new NhNonParameterExpression((ConstantExpression)expression.Arguments[1]));
            }
        }

        protected override string MappingsAssembly
        {
            get { return "NHibernate.Test"; }
        }

        protected override IList Mappings
        {
            get
            {
                return new[]
					{
						"NHSpecificTest.NH2658.Mappings.hbm.xml"
					};
            }
        }

        protected override void BuildSessionFactory()
        {
            base.BuildSessionFactory();
        
            //add our linq extension
            sessions.Settings.LinqToHqlGeneratorsRegistry.Merge(new DynamicPropertyGenerator());
        }

        [Test]
        public void Does_Not_Cache_NonParameters()
        {
            using (var session = OpenSession())
            {
                //PASSES
                using (var spy = new SqlLogSpy())
                {
                    //Query by name
                    var products =
                        (from p in session.Query<Product>() where p.GetProperty<string>("Name") == "Value" select p).ToList();

                    Assert.That(spy.GetWholeLog(), Is.StringContaining("Name=@p0"));
                }

                //FAILS
                //Because this query is considered the same as the top query the hql will be reused from the top statement
                //Even though GetProperty has a parameter that never get passed to sql or hql
                using (var spy = new SqlLogSpy())
                {
                    //Query by description
                    var products =
                        (from p in session.Query<Product>() where p.GetProperty<string>("Description") == "Value" select p).ToList();

                    Assert.That(spy.GetWholeLog(), Is.StringContaining("Description=@p0"));
                }               
            }
        }

        [Test]
        public void NhNonParameterExpressionCasts()
        {
            var constant = Expression.Constant("x");

            var nonParameter = new NhNonParameterExpression(constant);

            Assert.AreEqual(constant, (ConstantExpression)nonParameter);
        }

        [Test]
        public void CanExtendQueryNodes()
        {
            using (var session = OpenSession())
            using(var spy = new SqlLogSpy())
            {
                var products = (from p in session.Query<Product>() select p).TestExtension(p => p.Name == "Value").ToList();

                Assert.That(spy.GetWholeLog(), Is.StringContaining("Name=@p0"));
            }
        }
    }
}