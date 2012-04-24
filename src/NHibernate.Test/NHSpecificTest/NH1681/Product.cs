namespace NHibernate.Test.NHSpecificTest.NH1681
{
    public class Product
    {
        public virtual string ProductId { get; set; }

        public virtual int Id { get; set; }

        public virtual string Name { get; set; }

        public virtual string Description { get; set; }
    }
}
