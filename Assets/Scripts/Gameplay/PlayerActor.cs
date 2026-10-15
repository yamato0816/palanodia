using UnityEngine;
using Palinodia.Simulation;

namespace Palinodia.Gameplay
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class PlayerActor : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 6.1f;
        [SerializeField] private float jumpHeight = 1.15f;
        private CharacterController motor;
        private FollowCamera cameraRig;
        private float verticalVelocity;

        public PhysicalBodyState Body { get; private set; }
        public Vector3 ChestPosition => transform.position + Vector3.up * 1.35f;

        public void Initialize(FollowCamera rig, PhysicalBodyState body)
        {
            cameraRig = rig;
            Body = body;
        }

        private void Awake() => motor = GetComponent<CharacterController>();

        private void Update()
        {
            if (Body != null && !Body.Operational) return;
            if (cameraRig == null) return;
            Vector3 forward = cameraRig.FlatForward;
            Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
            Vector3 intent = (forward * Input.GetAxisRaw("Vertical") + right * Input.GetAxisRaw("Horizontal"));
            if (intent.sqrMagnitude > 1) intent.Normalize();
            if (intent.sqrMagnitude > .001f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(intent), 12f * Time.deltaTime);
            if (motor.isGrounded && verticalVelocity < 0) verticalVelocity = -2;
            if (Input.GetKeyDown(KeyCode.Space) && motor.isGrounded)
                verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * Physics.gravity.y);
            verticalVelocity += Physics.gravity.y * Time.deltaTime;
            motor.Move((intent * moveSpeed + Vector3.up * verticalVelocity) * Time.deltaTime);
        }
    }
}
