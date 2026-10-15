using UnityEngine;

namespace Palinodia.Gameplay
{
    [RequireComponent(typeof(Camera))]
    public sealed class FollowCamera : MonoBehaviour
    {
        private Transform target;
        private float yaw = 0f;
        private float pitch = 16f;
        private float distance = 5.2f;
        private bool cursorLocked;

        public Camera View { get; private set; }
        public Vector3 FlatForward => (Quaternion.Euler(0, yaw, 0) * Vector3.forward).normalized;

        public void Initialize(Transform player)
        {
            target = player;
            View = GetComponent<Camera>();
            SetCursorLocked(true);
            transform.position = target.position + new Vector3(0, 3, -6);
        }

        private void SetCursorLocked(bool value)
        {
            cursorLocked = value;
            Cursor.lockState = value ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !value;
        }

        private void LateUpdate()
        {
            if (target == null) return;
            if (Input.GetKeyDown(KeyCode.Escape)) SetCursorLocked(false);
            else if (!cursorLocked && Input.GetMouseButtonDown(0)) SetCursorLocked(true);

            if (cursorLocked)
            {
                yaw += Input.GetAxis("Mouse X") * 3.5f;
                pitch = Mathf.Clamp(pitch - Input.GetAxis("Mouse Y") * 2.8f, -22, 67);
            }
            Quaternion angle = Quaternion.Euler(pitch, yaw, 0);
            Vector3 point = target.position + Vector3.up * 1.65f;
            Vector3 desiredOffset = -(angle * Vector3.forward) * distance;
            float actualDistance = distance;
            if (Physics.SphereCast(point, .25f, desiredOffset.normalized, out RaycastHit hit,
                distance, ~0, QueryTriggerInteraction.Ignore))
            {
                if (!hit.collider.transform.IsChildOf(target)) actualDistance = Mathf.Max(1.0f, hit.distance - .2f);
            }
            transform.position = Vector3.Lerp(transform.position, point + desiredOffset.normalized * actualDistance,
                18f * Time.deltaTime);
            transform.rotation = Quaternion.LookRotation(point - transform.position, Vector3.up);
        }
    }
}
