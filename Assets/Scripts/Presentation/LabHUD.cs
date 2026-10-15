using UnityEngine;
using Palinodia.Gameplay;
using Palinodia.Simulation;

namespace Palinodia.Presentation
{
    public sealed class LabHUD : MonoBehaviour
    {
        private AbilityController ability;
        private LabObjective objective;
        private PlayerActor player;
        private StageEnvironmentState stage;
        private GUIStyle heading;
        private GUIStyle body;
        private GUIStyle small;
        private GUIStyle big;

        public void Initialize(AbilityController controller, LabObjective mission, PlayerActor hero, StageEnvironmentState environment)
        {
            ability = controller;
            objective = mission;
            player = hero;
            stage = environment;
        }

        private void Styles()
        {
            if (heading != null) return;
            heading = new GUIStyle(GUI.skin.label) { fontSize = 22, fontStyle = FontStyle.Bold };
            body = new GUIStyle(GUI.skin.label) { fontSize = 14 };
            small = new GUIStyle(GUI.skin.label) { fontSize = 12 };
            big = new GUIStyle(GUI.skin.label) { fontSize = 18, alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold, wordWrap = true };
            heading.normal.textColor = new Color(.55f, .9f, 1f);
            body.normal.textColor = Color.white;
            small.normal.textColor = new Color(.78f, .83f, .85f);
            big.normal.textColor = Color.white;
        }

        private static void RectColor(Rect r, Color color)
        {
            Color old = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = old;
        }

        private void OnGUI()
        {
            if (ability == null || player == null || objective == null || stage == null) return;
            Styles();
            float width = Mathf.Min(485, Screen.width - 24);
            RectColor(new Rect(12, 12, width, 185), new Color(.045f, .09f, .13f, .87f));
            GUI.Label(new Rect(24, 19, width - 20, 31), "PALINODIA   /   EXPERIMENT 01", heading);
            GUI.Label(new Rect(24, 54, width - 30, 23), "ENERGY RESERVE", small);
            RectColor(new Rect(24, 78, width - 48, 17), new Color(.12f, .20f, .24f));
            RectColor(new Rect(24, 78, (width - 48) * (float)ability.Reservoir.Fill, 17), new Color(.12f, .78f, .9f));
            GUI.Label(new Rect(26, 97, width - 48, 23),
                (ability.Reservoir.StoredJ / 1000).ToString("F1") + " / " +
                (ability.Reservoir.CapacityJ / 1000).ToString("F0") + " kJ", small);
            GUI.Label(new Rect(24, 126, width - 36, 24),
                "Body: " + player.Body.TemperatureC.ToString("F1") + " °C   |   Room: " +
                stage.TemperatureC.ToString("F1") + " °C   |   " + (stage.PressurePa / 1000).ToString("F1") + " kPa", body);
            GUI.Label(new Rect(24, 151, width - 36, 30),
                "Drones active: " + objective.Remaining + " / 2", body);

            float crossX = Screen.width * .5f;
            float crossY = Screen.height * .5f;
            RectColor(new Rect(crossX - 9, crossY, 18, 2), new Color(.7f, .95f, 1f, .85f));
            RectColor(new Rect(crossX, crossY - 9, 2, 18), new Color(.7f, .95f, 1f, .85f));

            if (Input.GetKey(KeyCode.Tab) && ability.AimedBody != null)
            {
                PhysicalBodyState target = ability.AimedBody;
                RectColor(new Rect(crossX - 165, crossY + 30, 330, 90), new Color(.02f, .13f, .18f, .91f));
                GUI.Label(new Rect(crossX - 151, crossY + 39, 310, 24), target.DisplayName, body);
                GUI.Label(new Rect(crossX - 151, crossY + 62, 310, 23), "Temperature " +
                    target.TemperatureC.ToString("F1") + " °C   |   " +
                    (target.Operational ? "FUNCTIONAL" : "INACTIVE"), small);
                GUI.Label(new Rect(crossX - 151, crossY + 84, 310, 22),
                    "Extractable heat " + (target.MaxRemovableHeatJ / 1000).ToString("F1") + " kJ", small);
            }

            float panelW = Mathf.Min(Screen.width - 24, 680);
            float left = (Screen.width - panelW) / 2f;
            RectColor(new Rect(left, Screen.height - 119, panelW, 105), new Color(.03f, .075f, .11f, .88f));
            GUI.Label(new Rect(left + 12, Screen.height - 112, panelW - 24, 27), objective.Status, big);
            GUI.Label(new Rect(left + 13, Screen.height - 79, panelW - 26, 24),
                "WASD Move     Mouse Aim     Space Jump     E Absorb     LMB Impulse     RMB Heat", small);
            GUI.Label(new Rect(left + 13, Screen.height - 52, panelW - 26, 22),
                "TAB Scan     R Restart     ESC Release Mouse", small);
            string feedback = ability.Feedback;
            if (!string.IsNullOrEmpty(feedback))
            {
                RectColor(new Rect(left, Screen.height - 160, panelW, 30), new Color(.05f, .13f, .2f, .8f));
                GUI.Label(new Rect(left + 12, Screen.height - 155, panelW - 24, 23), feedback, small);
            }
            if (objective.Lost || objective.Finished)
            {
                RectColor(new Rect(Screen.width * .22f, Screen.height * .38f,
                    Screen.width * .56f, 85), new Color(.02f, .07f, .11f, .94f));
                GUI.Label(new Rect(Screen.width * .25f, Screen.height * .4f,
                    Screen.width * .5f, 65), objective.Status, big);
            }
        }
    }
}
