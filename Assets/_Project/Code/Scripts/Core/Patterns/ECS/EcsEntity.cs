namespace Core.ECS
{
    public struct EcsEntity
    {
        public long Id;

        public EcsEntity(long id)
        {
            Id = id;
        }

        public override bool Equals(object obj)
        {
            return obj is EcsEntity entity && Id == entity.Id;
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }
    }
}
