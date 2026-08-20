using Photon.Pun;
using RepoValuableParry.Core;
using UnityEngine;

namespace RepoValuableParry.Networking
{
    internal sealed class ParryNetworkBehaviour : MonoBehaviourPun
    {
        public PlayerAvatar Player;

        [PunRPC]
        public void VP_ParryStart(int sequenceId, int playerViewId, int enemyViewId, int valuableViewId, float energy, Vector3 contact, int seed, PhotonMessageInfo info)
        {
            if (ParryManager.Instance == null)
                return;
            ParryManager.Instance.ReceiveRemoteParryStart(new ParryEvent
            {
                SequenceId = sequenceId,
                PlayerViewId = playerViewId,
                EnemyViewId = enemyViewId,
                ValuableViewId = valuableViewId,
                AttackEnergy = energy,
                ContactPoint = contact,
                EffectSeed = seed
            });
        }

        [PunRPC]
        public void VP_ParryCommit(int playerViewId, int enemyViewId, int valuableViewId, float energy, Vector3 contact, int seed, PhotonMessageInfo info)
        {
            if (ParryManager.Instance == null || !SemiFunc.IsMasterClient())
                return;
            ParryManager.Instance.HostCommitParry(playerViewId, enemyViewId, valuableViewId, energy, contact, seed);
        }
    }

    internal static class ParryNetworkManager
    {
        public static void EnsureOnPlayer(PlayerAvatar player)
        {
            if (player == null)
                return;
            if (player.GetComponent<ParryNetworkBehaviour>() == null)
            {
                var behaviour = player.gameObject.AddComponent<ParryNetworkBehaviour>();
                behaviour.Player = player;
            }
        }

        public static int ViewId(Component component)
        {
            if (component == null)
                return 0;
            var view = component.GetComponent<PhotonView>();
            if (view == null)
                view = component.GetComponentInParent<PhotonView>();
            return view != null ? view.ViewID : 0;
        }

        public static void BroadcastParryStart(ParryContext context)
        {
            if (!SemiFunc.IsMultiplayer() || !SemiFunc.IsMasterClient())
                return;
            var behaviour = RpcHost();
            if (behaviour == null || behaviour.photonView == null)
                return;

            behaviour.photonView.RPC(
                nameof(ParryNetworkBehaviour.VP_ParryStart),
                RpcTarget.Others,
                context.SequenceId,
                ViewId(context.Player),
                context.Enemy != null ? context.Enemy.photonView.ViewID : 0,
                ViewId(context.PhysObject),
                context.AttackEnergy,
                context.ContactPoint,
                context.EffectSeed);
        }

        public static void SendCommitToHost(ParryContext context)
        {
            if (!SemiFunc.IsMultiplayer() || SemiFunc.IsMasterClient())
                return;
            var behaviour = context.Player != null
                ? context.Player.GetComponent<ParryNetworkBehaviour>()
                : null;
            if (behaviour == null || behaviour.photonView == null)
                return;

            behaviour.photonView.RPC(
                nameof(ParryNetworkBehaviour.VP_ParryCommit),
                RpcTarget.MasterClient,
                ViewId(context.Player),
                context.Enemy != null ? context.Enemy.photonView.ViewID : 0,
                ViewId(context.PhysObject),
                context.AttackEnergy,
                context.ContactPoint,
                context.EffectSeed);
        }

        static ParryNetworkBehaviour RpcHost()
        {
            var local = SemiFunc.PlayerGetLocal();
            return local != null ? local.GetComponent<ParryNetworkBehaviour>() : null;
        }
    }
}
