using System.Collections;
using NHibernate.Cfg;
using NHibernate.Criterion;
using NUnit.Framework;

namespace NHibernate.Test.NHSpecificTest.NH1681
{
    [TestFixture]
    public class Fixture : TestCase
    {
        protected override string MappingsAssembly
        {
            get { return "NHibernate.Test"; }
        }

        protected override IList Mappings
        {
            get
            {
                return new string[]
					{
						"NHSpecificTest.NH1681.Mappings.hbm.xml"
					};
            }
        }

        protected override void Configure(Cfg.Configuration configuration)
        {
	        base.Configure(configuration);

            configuration.SetProperty(Environment.FormatSql, "false");
        }

        /// <summary>
        /// push some data into the database
        /// Really functions as a save test also 
        /// </summary>
        protected override void OnSetUp()
        {
            base.OnSetUp();

            using (var session = OpenSession())
            {
                using (var tran = session.BeginTransaction())
                {
                    Product product = new Product();
                    product.ProductId = "XO1234";
                    product.Id = 1;
                    product.Name = "Some product";
                    product.Description = "Very good";

                    session.Save(product);

                    product = new Product();
                    product.ProductId = "XO54321";
                    product.Id = 2;
                    product.Name = "Other product";
                    product.Description = "Very bad";

                    session.Save(product);

                    tran.Commit();
                }
            }
        }

        protected override void OnTearDown()
        {
            base.OnTearDown();

            using (var session = OpenSession())
            {
                using (var tran = session.BeginTransaction())
                {
                    session.Delete("from Product");
                    tran.Commit();
                }                
            }

        }

        [Test]
        public void Query_records()
        {
            using (var sqlLog = new SqlLogSpy())
            using (var session = OpenSession())
            {
                Product product = session.CreateCriteria(typeof (Product))
                    .Add(Restrictions.Eq("ProductId", "XO1234"))
                    .UniqueResult<Product>();

                Assert.IsNotNull(product);
                Assert.AreEqual("Very good", product.Description);

                var log = sqlLog.GetWholeLog();
                //needs to be joining on the Id column not the productId
                Assert.IsTrue(log.Contains("inner join ProductLocalized this_1_ on this_.Id=this_1_.Id"));
            }
        }

        [Test]
        public void Update_record()
        {
            using (var session = OpenSession())
            {
                Product product = session.CreateCriteria(typeof(Product))
                    .Add(Restrictions.Eq("ProductId", "XO1234"))
                    .UniqueResult<Product>();

                Assert.IsNotNull(product);

                product.Name = "TestValue";
                product.Description = "TestValue";

                session.Flush();
                session.Clear();

                //pull again
                product = session.CreateCriteria(typeof(Product))
                    .Add(Restrictions.Eq("ProductId", "XO1234"))
                    .UniqueResult<Product>();

                Assert.IsNotNull(product);
                Assert.AreEqual("TestValue", product.Name);
                Assert.AreEqual("TestValue", product.Description);
            }
        }

        [Test]
        public void Delete_single_record()
        {
            using (var session = OpenSession())
            {
                Product product = new Product();
                product.ProductId = "XO1111";
                product.Id = 3;
                product.Name = "Test";
                product.Description = "Test";

                session.Save(product);

                session.Flush();

                session.Delete(product);
                session.Flush();

                session.Clear();

                //try to query for this product
                product = session.CreateCriteria(typeof(Product))
                    .Add(Restrictions.Eq("ProductId", "XO1111"))
                    .UniqueResult<Product>();

                Assert.IsNull(product);
            }
        }
    }
}
