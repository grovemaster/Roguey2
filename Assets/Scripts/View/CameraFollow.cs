using JRogue.Actors;
using JRogue.Manager.Party;
using UnityEngine;

namespace JRogue.View
{
    public class CameraFollow : MonoBehaviour
    {
        Transform _manualTarget;
        [SerializeField] float smoothSpeed = 0.125f;
        [SerializeField] Vector3 offset = new Vector3(0, 0, z: -10);
        [SerializeField] bool preferPartyActiveMember = true;

        public void SetTarget(Transform newTarget)
        {
            _manualTarget = newTarget;
        }

        void LateUpdate()
        {
            Transform follow = ResolveFollowTarget();
            if (follow == null)
                return;

            Vector3 desiredPosition = follow.position + offset;
            transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);
        }

        Transform ResolveFollowTarget()
        {
            if (preferPartyActiveMember && PartyManager.Instance != null)
            {
                BaseActor active = PartyManager.Instance.GetActiveMember();
                if (active != null)
                    return active.transform;
            }

            return _manualTarget;
        }
    }
}
