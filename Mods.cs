using UnityEngine;

namespace modValheim
{
    class Mods : MonoBehaviour
    {
        public void OnGUI()
        {
            //Replace OnlinePlayer with the name of your Player class, and make sure to change the reference DLLs to the ones for your game!

            foreach (AnimalAI animals in FindObjectsOfType(typeof(AnimalAI)) as AnimalAI[])
            {
                //In-Game Position
                Vector3 pivotPos = animals.transform.position; //Pivot point NOT at the feet, at the center
                Vector3 animalsFootPos; animalsFootPos.x = pivotPos.x; animalsFootPos.z = pivotPos.z; animalsFootPos.y = pivotPos.y - 2f; //At the feet
                Vector3 animalsHeadPos; animalsHeadPos.x = pivotPos.x; animalsHeadPos.z = pivotPos.z; animalsHeadPos.y = pivotPos.y + 2f; //At the head

                //Screen Position
                Vector3 w2s_footpos = Camera.main.WorldToScreenPoint(animalsFootPos);
                Vector3 w2s_headpos = Camera.main.WorldToScreenPoint(animalsHeadPos);

                if (w2s_footpos.z > 0f)
                {
                    DrawBoxESP(w2s_footpos, w2s_headpos, Color.green);
                }
            }

            foreach (Player player in FindObjectsOfType(typeof(Player)) as Player[])
            {
                //In-Game Position
                Vector3 pivotPos = player.transform.position; //Pivot point NOT at the feet, at the center
                Vector3 playerFootPos; playerFootPos.x = pivotPos.x; playerFootPos.z = pivotPos.z; playerFootPos.y = pivotPos.y - 2f; //At the feet
                Vector3 playerHeadPos; playerHeadPos.x = pivotPos.x; playerHeadPos.z = pivotPos.z; playerHeadPos.y = pivotPos.y + 2f; //At the head

                //Screen Position
                Vector3 w2s_footpos = Camera.main.WorldToScreenPoint(playerFootPos);
                Vector3 w2s_headpos = Camera.main.WorldToScreenPoint(playerHeadPos);

                GUI.Label(new Rect(w2s_headpos.x, Screen.height - w2s_headpos.y, 100, 20), "Player");

                if (w2s_footpos.z > 0f)
                {
                    DrawBoxESP(w2s_footpos, w2s_headpos, Color.green);
                }
            }

            foreach (BaseAI baseai in FindObjectsOfType(typeof(BaseAI)) as BaseAI[])
            {
                //In-Game Position
                Vector3 pivotPos = baseai.transform.position; //Pivot point NOT at the feet, at the center
                Vector3 baseaiFootPos; baseaiFootPos.x = pivotPos.x; baseaiFootPos.z = pivotPos.z; baseaiFootPos.y = pivotPos.y - 2f; //At the feet
                Vector3 baseaiHeadPos; baseaiHeadPos.x = pivotPos.x; baseaiHeadPos.z = pivotPos.z; baseaiHeadPos.y = pivotPos.y + 2f; //At the head

                //Screen Position
                Vector3 w2s_footpos = Camera.main.WorldToScreenPoint(baseaiFootPos);
                Vector3 w2s_headpos = Camera.main.WorldToScreenPoint(baseaiHeadPos);

                GUI.Label(new Rect(w2s_headpos.x, Screen.height - w2s_headpos.y, 100, 20), "Ennemy");

                if (w2s_footpos.z > 0f)
                {
                    DrawBoxESP(w2s_footpos, w2s_headpos, Color.red);
                }
            }
        }

        public void DrawBoxESP(Vector3 footpos, Vector3 headpos, Color color) //Rendering the ESP
        {
            float height = headpos.y - footpos.y;
            float widthOffset = 2f;
            float width = height / widthOffset;

            //ESP BOX
            Render.DrawBox(footpos.x - (width / 2), (float)Screen.height - footpos.y - height, width, height, color, 2f);

            //Snapline
            Render.DrawLine(new Vector2((float)(Screen.width / 2), (float)(Screen.height / 2)), new Vector2(footpos.x, (float)Screen.height - footpos.y), color, 2f);
        }

        public void Update()
        {
            if (Input.GetKeyDown(KeyCode.Delete))
            {
                Loader.Unload();
            }
        }
    }
}
