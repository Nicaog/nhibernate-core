using System.Linq;
using NHibernate.Linq;
using NUnit.Framework;

namespace NHibernate.Test.NHSpecificTest.NH2852
{
    [TestFixture]
    public class Fixture : BugTestCase
    {
        [Test]
        public void ThenFetchCanExecute()
        {
            using (var session = OpenSession())
            {
                var query = session.Query<Person>()
                    .Where(p => p.Address.City.Name == "Test")
                    .Fetch(r => r.Address)
                    .ThenFetch(a => a.City);

                var results = query.ToList();
            }
        }

        [Test]
        public void AlsoFails()
        {
            using (var session = OpenSession())
            {
                var query = session.Query<Person>()
                    .Where(p => p.Parent.Parent.Name == "Test")
                    .Fetch(p => p.Parent)
                    .ThenFetch(p => p.Parent);

                var results = query.ToList();
            }
        }

        [Test]
        public void Passes()
        {
            using (var session = OpenSession())
            {
                var query = session.Query<Person>()
                    .Where(p => p.Address.Name == "Test")
                    .Fetch(r => r.Address)
                    .ThenFetch(a => a.City);

                var results = query.ToList();
            }
        }

        [Test]
        public void AlsoPasses()
        {
            using (var session = OpenSession())
            {
                var query = session.Query<Person>()
                    .Where(p => p.Address.City.Name == "Test")
                    .Fetch(r => r.Address);

                var results = query.ToList();
            }
        }
    }
}
