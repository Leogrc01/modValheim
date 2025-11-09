using System.Collections.Generic;
using UnityEngine;

namespace modValheim
{
    class Mods : MonoBehaviour
    {
        private Camera mainCamera;
        private List<BaseAI> aiList = new List<BaseAI>();

        private void Start()
        {
            mainCamera = Camera.main;
        }

        public void OnGUI()
        {
            // On dessine seulement ici, pas de recherche coûteuse
            foreach (BaseAI ai in aiList)
            {
                if (ai != null)
                {
                    DrawAIESP(ai);
                }
            }
        }

        public void DrawAIESP(BaseAI ai)
        {
            Bounds bounds = ai.GetComponentInChildren<Renderer>().bounds;
            Vector3 baseAIFootPos = new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
            Vector3 baseAIHeadPos = new Vector3(bounds.center.x, bounds.max.y, bounds.center.z);

            Vector3 w2sFootPos = mainCamera.WorldToScreenPoint(baseAIFootPos);
            Vector3 w2sHeadPos = mainCamera.WorldToScreenPoint(baseAIHeadPos);

            if (w2sFootPos.z > 0f)
            {
                DrawBoxESP(w2sFootPos, w2sHeadPos, Color.red);
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

            // On met à jour la liste des AI seulement 1 fois par frame
            aiList.Clear();
            BaseAI[] allAI = FindObjectsOfType(typeof(BaseAI)) as BaseAI[];
            if (allAI != null)
            {
                aiList.AddRange(allAI);
            }
        }
    }
}
