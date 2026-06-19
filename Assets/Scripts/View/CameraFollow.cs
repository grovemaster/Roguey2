using JRogue.Actors;
using JRogue.Manager.Party;
using JRogue.UI.Gameplay;
using UnityEngine;

namespace JRogue.View
{
    public class CameraFollow : MonoBehaviour
    {
        Transform _manualTarget;
        [SerializeField] float smoothSpeed = 0.125f;
        [SerializeField] Vector3 offset = new Vector3(0, 0, z: -10);
        [SerializeField] bool preferPartyActiveMember = true;

        public void SetTarget(Transform newTarget, bool snapImmediate = true)
        {
            _manualTarget = newTarget;
            if (snapImmediate)
                SnapToTarget();
        }

        public void SnapToTarget()
        {
            Transform follow = ResolveFollowTarget();
            if (follow == null)
                return;

            Vector3 desiredPosition = follow.position + offset;
            desiredPosition.y += PlayfieldLayout.GetCameraVerticalOffsetWorld(GetComponent<Camera>());
            transform.position = desiredPosition;
        }

        void LateUpdate()
        {
            Transform follow = ResolveFollowTarget();
            if (follow == null)
                return;

            Vector3 desiredPosition = follow.position + offset;
            desiredPosition.y += PlayfieldLayout.GetCameraVerticalOffsetWorld(GetComponent<Camera>());
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
