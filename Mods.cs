using System.Collections.Generic;
using UnityEngine;

namespace modValheim
{
    class Mods : MonoBehaviour
    {
        private Camera mainCamera;
        private List<BaseAI> aiList = new List<BaseAI>();
        private List<AnimalAI> animalList = new List<AnimalAI>();
        private List<ItemDrop> itemList = new List<ItemDrop>();
        private MenuGUI menuGUI;

        private void Start()
        {
            mainCamera = Camera.main;
            
            // Créer le menu GUI
            menuGUI = gameObject.AddComponent<MenuGUI>();
        }

        public void OnGUI()
        {
            // On dessine seulement ici, pas de recherche coûteuse
            if (menuGUI.ShowEnemies)
            {
                foreach (BaseAI ai in aiList)
                {
                    if (ai != null)
                    {
                        DrawEntityESP(ai, Color.red);
                    }
                }
            }
            
            if (menuGUI.ShowAnimals)
            {
                foreach (AnimalAI animalai in animalList)
                {
                    if (animalai != null)
                    {
                        DrawEntityESP(animalai, Color.green);
                    }
                }
            }
            
            if (menuGUI.ShowItems)
            {
                foreach (ItemDrop item in itemList)
                {
                    if (item != null)
                    {
                        DrawEntityESP(item, Color.cyan, true); // Bleu cyan avec nom
                    }
                }
            }
        }

        // Fonction universelle pour dessiner l'ESP de n'importe quelle entité
        public void DrawEntityESP(Component entity, Color color, bool showName = false)
        {
            Renderer renderer = entity.GetComponentInChildren<Renderer>();
            if (renderer == null) return;

            Bounds bounds = renderer.bounds;
            Vector3 footPos = new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
            Vector3 headPos = new Vector3(bounds.center.x, bounds.max.y, bounds.center.z);

            Vector3 w2sFootPos = mainCamera.WorldToScreenPoint(footPos);
            Vector3 w2sHeadPos = mainCamera.WorldToScreenPoint(headPos);

            if (w2sFootPos.z > 0f)
            {
                string name = showName ? GetEntityName(entity) : null;
                DrawBoxESP(w2sFootPos, w2sHeadPos, color, name);
            }
        }

        private string GetEntityName(Component entity)
        {
            // Essayer d'obtenir le nom de l'item
            if (entity is ItemDrop itemDrop && itemDrop.m_itemData != null)
            {
                return itemDrop.m_itemData.m_shared.m_name;
            }
            
            // Sinon retourner le nom du GameObject
            return entity.gameObject.name.Replace("(Clone)", "").Trim();
        }

        public void DrawBoxESP(Vector3 footpos, Vector3 headpos, Color color, string name = null) //Rendering the ESP
        {
            float height = headpos.y - footpos.y;
            float widthOffset = 2f;
            float width = height / widthOffset;

            //ESP BOX
            if (menuGUI.ShowBoxes)
            {
                Render.DrawBox(footpos.x - (width / 2), (float)Screen.height - footpos.y - height, width, height, color, 2f);
            }

            // Afficher le nom au-dessus de la boîte
            if (!string.IsNullOrEmpty(name))
            {
                Vector2 namePos = new Vector2(footpos.x, (float)Screen.height - footpos.y - height - 15);
                DrawText(namePos, name, color);
            }

            //Snapline
            if (menuGUI.ShowSnaplines)
            {
                Render.DrawLine(new Vector2((float)(Screen.width / 2), (float)(Screen.height / 2)), new Vector2(footpos.x, (float)Screen.height - footpos.y), color, 2f);
            }
        }

        private void DrawText(Vector2 position, string text, Color color)
        {
            GUIStyle style = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };

            GUIContent content = new GUIContent(text);
            Vector2 size = style.CalcSize(content);
            Rect rect = new Rect(position.x - size.x / 2, position.y - size.y / 2, size.x, size.y);

            // Ombre pour meilleure lisibilité
            style.normal.textColor = Color.black;
            GUI.Label(new Rect(rect.x + 1, rect.y + 1, rect.width, rect.height), content, style);

            // Texte principal
            style.normal.textColor = color;
            GUI.Label(rect, content, style);
        }

        public void Update()
        {
            if (Input.GetKeyDown(KeyCode.Delete))
            {
                Loader.Unload();
            }

            // On met à jour la liste des AI seulement 1 fois par frame
            aiList.Clear();
            animalList.Clear();
            itemList.Clear();
            
            BaseAI[] allAI = FindObjectsOfType(typeof(BaseAI)) as BaseAI[];
            if (allAI != null)
            {
                aiList.AddRange(allAI);
            }
            
            AnimalAI[] animAI = FindObjectsOfType(typeof(AnimalAI)) as AnimalAI[];
            if (animAI != null)
            {
                animalList.AddRange(animAI);
            }
            
            ItemDrop[] items = FindObjectsOfType(typeof(ItemDrop)) as ItemDrop[];
            if (items != null)
            {
                itemList.AddRange(items);
            }
        }
    }
}
