using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace modValheim
{
    class Mods : MonoBehaviour
    {
        private Camera mainCamera;
        private List<BaseAI> aiList = new List<BaseAI>();
        private List<AnimalAI> animalList = new List<AnimalAI>();
        private List<ItemDrop> itemList = new List<ItemDrop>();
        private List<Player> playersList = new List<Player>();
        private List<GameObject> bossStoneList = new List<GameObject>();
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
                        DrawEntityESP(ai, Color.red, true, menuGUI.ShowDistances, menuGUI.MaxEnemyDistance);
                    }
                }
            }

            if (menuGUI.ShowPlayers)
            {
                foreach (Player players in playersList)
                {
                    if (players != null)
                    {
                        DrawEntityESP(players, Color.yellow, true, menuGUI.ShowDistances, menuGUI.MaxEnemyDistance);
                    }
                }
            }
            
            if (menuGUI.ShowAnimals)
            {
                foreach (AnimalAI animalai in animalList)
                {
                    if (animalai != null)
                    {
                        DrawEntityESP(animalai, Color.green, true, menuGUI.ShowDistances, menuGUI.MaxAnimalDistance);
                    }
                }
            }
            
            if (menuGUI.ShowItems)
            {
                foreach (ItemDrop item in itemList)
                {
                    if (item != null)
                    {
                        DrawEntityESP(item, Color.cyan, true, menuGUI.ShowDistances, menuGUI.MaxItemDistance); // Bleu cyan avec nom
                    }
                }
            }
            
            if (menuGUI.ShowBossStones)
            {
                foreach (GameObject bossStone in bossStoneList)
                {
                    if (bossStone != null)
                    {
                        DrawBossStoneESP(bossStone, Color.magenta, menuGUI.ShowDistances, menuGUI.MaxBossStoneDistance);
                    }
                }
            }
        }

        // Fonction pour dessiner l'ESP des BossStones
        public void DrawBossStoneESP(GameObject bossStone, Color color, bool showDistance, float maxDistance)
        {
            // Vérifier la distance
            float distance = Vector3.Distance(mainCamera.transform.position, bossStone.transform.position);
            if (distance > maxDistance) return;

            Renderer renderer = bossStone.GetComponentInChildren<Renderer>();
            if (renderer == null) return;

            Bounds bounds = renderer.bounds;
            Vector3 footPos = new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
            Vector3 headPos = new Vector3(bounds.center.x, bounds.max.y, bounds.center.z);

            Vector3 w2sFootPos = mainCamera.WorldToScreenPoint(footPos);
            Vector3 w2sHeadPos = mainCamera.WorldToScreenPoint(headPos);

            if (w2sFootPos.z > 0f)
            {
                string name = bossStone.name.Replace("(Clone)", "").Trim();
                string distanceText = showDistance ? $" [{distance:F1}m]" : "";
                DrawBoxESP(w2sFootPos, w2sHeadPos, color, name, distanceText);
            }
        }

        // Fonction universelle pour dessiner l'ESP de n'importe quelle entité
        public void DrawEntityESP(Component entity, Color color, bool showName = false, bool showDistance = true, float maxDistance = 100f)
        {
            // Vérifier la distance
            float distance = Vector3.Distance(mainCamera.transform.position, entity.transform.position);
            if (distance > maxDistance) return;

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
                string distanceText = showDistance ? $" [{distance:F1}m]" : "";
                DrawBoxESP(w2sFootPos, w2sHeadPos, color, name, distanceText);
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

        public void DrawBoxESP(Vector3 footpos, Vector3 headpos, Color color, string name = null, string distanceText = null) //Rendering the ESP
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
            if (!string.IsNullOrEmpty(name) || !string.IsNullOrEmpty(distanceText))
            {
                string displayText = name + distanceText;
                Vector2 namePos = new Vector2(footpos.x, (float)Screen.height - footpos.y - height - 15);
                DrawText(namePos, displayText, color);
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

            // Gestion des skills
            ApplySkillModifications();
            
            // Réinitialiser les skills si demandé
            if (menuGUI.ResetSkillsRequested)
            {
                ResetAllSkills();
                menuGUI.ResetSkillsRequested = false;
            }

            // Stamina infinie
            if (menuGUI.UnlimitedStamina)
            {
                Player localPlayer = Player.m_localPlayer;
                if (localPlayer != null)
                {
                    // Utiliser la méthode publique pour ajouter de la stamina
                    float maxStamina = localPlayer.GetMaxStamina();
                    float currentStamina = localPlayer.GetStamina();
                    
                    if (currentStamina < maxStamina)
                    {
                        localPlayer.AddStamina(maxStamina - currentStamina);
                    }
                }
            }

            // On met à jour la liste des AI seulement 1 fois par frame
            aiList.Clear();
            animalList.Clear();
            itemList.Clear();
            bossStoneList.Clear();
            
            // Récupérer tous les joueurs (y compris le joueur local et les autres joueurs)
            playersList.Clear();
            List<Player> allPlayers = Player.GetAllPlayers();
            if (allPlayers != null)
            {
                playersList.AddRange(allPlayers);
            }
            
            // Récupérer d'abord les animaux
            AnimalAI[] animAI = FindObjectsOfType(typeof(AnimalAI)) as AnimalAI[];
            if (animAI != null)
            {
                animalList.AddRange(animAI);
            }
            
            // Ensuite récupérer les AI en excluant les animaux
            BaseAI[] allAI = FindObjectsOfType(typeof(BaseAI)) as BaseAI[];
            if (allAI != null)
            {
                foreach (BaseAI ai in allAI)
                {
                    // Ne pas ajouter si c'est un AnimalAI (pour éviter les doublons avec les ennemis)
                    if (!(ai is AnimalAI))
                    {
                        aiList.Add(ai);
                    }
                }
            }
            
            ItemDrop[] items = FindObjectsOfType(typeof(ItemDrop)) as ItemDrop[];
            if (items != null)
            {
                itemList.AddRange(items);
            }
            
            // Récupérer les BossStones (autels de boss) par leur nom
            // Les autels de boss ont généralement "BossStone" ou "Altar" dans leur nom
            GameObject[] allObjects = FindObjectsOfType<GameObject>();
            foreach (GameObject obj in allObjects)
            {
                if (obj.name.Contains("BossStone") || 
                    obj.name.Contains("altar") || 
                    obj.name.Contains("Altar") ||
                    obj.name.Contains("Offering"))
                {
                    bossStoneList.Add(obj);
                }
            }
        }

        private void ApplySkillModifications()
        {
            Player localPlayer = Player.m_localPlayer;
            if (localPlayer == null) return;

            Skills skills = localPlayer.GetSkills();
            if (skills == null) return;

            // Multiplicateur de compétences - utiliser la réflexion pour modifier le champ privé
            if (menuGUI.SkillMultiplier > 1f)
            {
                SetPrivateField(skills, "m_useSkillGainFactor", menuGUI.SkillMultiplier);
            }
            else
            {
                SetPrivateField(skills, "m_useSkillGainFactor", 1f);
            }

            // Empêcher la perte de skills à la mort - utiliser la réflexion
            if (menuGUI.NoSkillDrain)
            {
                SetPrivateField(skills, "m_DeathLowerFactor", 0f);
            }
            else
            {
                SetPrivateField(skills, "m_DeathLowerFactor", 0.25f);
            }
        }

        private void ResetAllSkills()
        {
            Player localPlayer = Player.m_localPlayer;
            if (localPlayer == null) return;

            Skills skills = localPlayer.GetSkills();
            if (skills == null) return;

            // Réinitialiser toutes les compétences à 0
            foreach (Skills.Skill skill in skills.GetSkillList())
            {
                skill.m_level = 0f;
                skill.m_accumulator = 0f;
            }
        }

        // Méthode utilitaire pour modifier des champs privés via réflexion
        private void SetPrivateField(object obj, string fieldName, object value)
        {
            FieldInfo field = obj.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            if (field != null)
            {
                field.SetValue(obj, value);
            }
        }
    }
}
