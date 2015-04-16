using System;
using System.Linq.Expressions;

namespace Takenet.SimplePersistence.Tests
{
    public abstract class GuidItemMapQueryableStorageFacts : MapQueryableStorageFacts<Guid, Item>
    {
        public override Expression<Func<Item, bool>> CreateFilter(Item value)
        {
            return
                i =>
                    i.GuidProperty.Equals(value.GuidProperty) && 
                    i.IntegerProperty.Equals(value.IntegerProperty) &&
                    i.StringProperty.Equals(value.StringProperty);
        }
    }
}