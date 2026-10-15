using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Palinodia.Simulation;

namespace Palinodia.Gameplay
{
    public sealed class LabObjective : MonoBehaviour
    {
        private readonly List<PhysicalBodyState> targets = new List<PhysicalBodyState>();
        private PlayerActor player;
        private GameObject door;
        private Renderer goalIndicator;
        private bool opened;
        public bool Finished { get; private set; }
        public bool Lost => player != null && player.Body != null && !player.Body.Operational;
        public int Remaining
        {
            get
            {
                int count = 0;
                foreach (PhysicalBodyState b in targets) if (b != null && b.Operational) count++;
                return count;
            }
        }
        public string Status => Lost ? "BIOSYSTEM FAILURE — PRESS R TO RESTART" :
            Finished ? "LAB CLEARED — ALL DRONES DISABLED. PRESS R TO RESTART" :
            opened ? "EXIT UNLOCKED — ENTER THE CYAN RING" :
            "DISABLE BOTH PURSUIT DRONES TO UNLOCK THE EXIT";

        public void Initialize(PlayerActor actor, GameObject lockedDoor, Renderer indicator, params PhysicalBodyState[] enemies)
        {
            player = actor;
            door = lockedDoor;
            goalIndicator = indicator;
            targets.AddRange(enemies);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.R))
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
                return;
            }
            if (Finished || Lost || player == null) return;
            if (!opened && Remaining == 0)
            {
                opened = true;
                if (door != null) door.SetActive(false);
                if (goalIndicator != null) goalIndicator.material.color = new Color(.12f, 1f, .65f);
            }
            if (opened && Vector3.Distance(player.transform.position, new Vector3(0, 0, 19.2f)) < 2.25f)
            {
                Finished = true;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }
    }
}
