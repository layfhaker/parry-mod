using RepoValuableParry.Core;

namespace RepoValuableParry.Detection
{
    internal static class HeldValuableDetector
    {
        public static bool TryGetHeldValuable(PlayerAvatar player, out PhysGrabObject phys, out ValuableObject valuable, out ParryRejectReason reject)
        {
            phys = null;
            valuable = null;
            reject = ParryRejectReason.NoHeldObject;

            if (player == null)
                return false;

            var grabber = player.physGrabber;
            if (grabber == null || !grabber.grabbed)
                return false;

            phys = GameAccess.GetGrabbed(grabber);
            if (phys == null)
                return false;

            valuable = phys.GetComponent<ValuableObject>();
            if (valuable == null)
            {
                reject = ParryRejectReason.NotValuable;
                return false;
            }

            reject = ParryRejectReason.None;
            return true;
        }

        public static bool TryGetLocalHeldValuable(out PhysGrabObject phys, out ValuableObject valuable, out ParryRejectReason reject)
        {
            phys = null;
            valuable = null;
            reject = ParryRejectReason.NoHeldObject;

            if (!SemiFunc.PhysGrabberLocalIsGrabbing())
                return false;

            phys = SemiFunc.PhysGrabberLocalGetGrabbedPhysGrabObject();
            if (phys == null)
                return false;

            valuable = phys.GetComponent<ValuableObject>();
            if (valuable == null)
            {
                reject = ParryRejectReason.NotValuable;
                return false;
            }

            reject = ParryRejectReason.None;
            return true;
        }
    }
}
