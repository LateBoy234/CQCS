using System;

namespace ThreeNH.SQCX.Domain.Common
{
    public abstract class Entity<TId> where TId : IEquatable<TId>
    {
        protected Entity(TId id)
        {
            Id = id;
        }

        public TId Id { get; private set; }

        public override bool Equals(object obj)
        {
            var other = obj as Entity<TId>;
            return other != null && GetType() == other.GetType() && Id.Equals(other.Id);
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }
    }
}
