using UnityEngine;

namespace RepoValuableParry.Core
{
    public sealed class ParryIntent
    {
        public PlayerAvatar Player;
        public PhysGrabObject PhysObject;
        public ValuableObject Valuable;
        public float CreatedAt;
        public float ExpiresAt;
        public int SequenceHint;

        public bool IsExpired => Time.time > ExpiresAt;

        public bool StillHoldsSameObject()
        {
            if (Player == null || PhysObject == null || PhysObject.dead)
                return false;
            var grabber = Player.physGrabber;
            if (grabber == null || !grabber.grabbed)
                return false;
            var current = GameAccess.GetGrabbed(grabber);
            return current == PhysObject;
        }
    }
}
