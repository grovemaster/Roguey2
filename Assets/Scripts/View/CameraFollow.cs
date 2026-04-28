using UnityEngine;

namespace JRogue.View
{
    public class CameraFollow : MonoBehaviour
    {
        private Transform target;
        [SerializeField] private float smoothSpeed = 0.125f;
        [SerializeField] private Vector3 offset = new Vector3(0, 0, -10);

        // This is the "Hook" that the PartyManager will call
        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
        }

        void LateUpdate()
        {
            if (target == null) return;

            // Simple Lerp for smooth following
            Vector3 desiredPosition = target.position + offset;
            Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);
            transform.position = smoothedPosition;
        }
    }
}