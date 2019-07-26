using GameSystem.GameCore.Physics;

namespace GameSystem.GameCore.Components
{
    public class SphereCollider : Collider, ISphereShape
    {
        private float radius = 0.5f;
        public float Radius { get { return radius; } }

        public override void Start()
        {
            colProxy = Manager.PhysicEngine.CreateSphereCollision(this, Layer, Mask);
            //radius.Lock();
            base.Start();
        }

        public void SetSize(float radius)
        {
            //this.radius.Set(radius);
            this.radius = radius;
        }
    }
}
