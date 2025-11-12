using System;
using System.Collections.Generic;
using System.Linq;
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
        private List<GameObject> bossStoneList = new List<GameObject>();
        private List<Player> playersList = new List<Player>();
        private MenuGUI menuGUI;
        
        // Optimisation: scanner moins fréquemment
        private float lastScanTime = 0f;
        private const float SCAN_INTERVAL = 1.0f; // Scanner toutes les 1 seconde (réduit les micro-lags)
        private GUIStyle textStyle; // Style de texte réutilisable
        private bool lastOneShotState = false; // Pour détecter les changements d'état
        
        // Valeurs originales pour le speedhack
        private Dictionary<string, float> originalSpeeds = new Dictionary<string, float>();
        private bool speedsStored = false;
        private float lastSpeedMultiplier = 1f;

        private void Start()
        {
            mainCamera = Camera.main;
            
            // Créer le menu GUI
            menuGUI = gameObject.AddComponent<MenuGUI>();
            
            // Initialiser le style de texte une seule fois
            textStyle = new GUIStyle()
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
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
            if (textStyle == null) return;

            GUIContent content = new GUIContent(text);
            Vector2 size = textStyle.CalcSize(content);
            Rect rect = new Rect(position.x - size.x / 2, position.y - size.y / 2, size.x, size.y);

            // Ombre pour meilleure lisibilité
            textStyle.normal.textColor = Color.black;
            GUI.Label(new Rect(rect.x + 1, rect.y + 1, rect.width, rect.height), content, textStyle);

            // Texte principal
            textStyle.normal.textColor = color;
            GUI.Label(rect, content, textStyle);
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

            // Dupliquer l'item du slot 8 si demandé
            if (menuGUI.DuplicateSlot8Requested)
            {
                DuplicateSlot8Item();
                menuGUI.DuplicateSlot8Requested = false;
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

            if (menuGUI.InfiniteHealth)
            {
                Player localPlayer = Player.m_localPlayer;
                if (localPlayer != null)
                {
                    // Définir la santé au maximum à chaque frame
                    float maxHealth = localPlayer.GetMaxHealth();
                    localPlayer.SetHealth(maxHealth);
                    
                    // Désactiver le mode God si ce n'est pas déjà fait
                    // Cela empêche les dégâts d'être appliqués
                    if (!localPlayer.InGodMode())
                    {
                        localPlayer.SetGodMode(true);
                    }
                }
            }
            else
            {
                // Désactiver le mode God si l'option est désactivée
                Player localPlayer = Player.m_localPlayer;
                if (localPlayer != null && localPlayer.InGodMode())
                {
                    localPlayer.SetGodMode(false);
                }
            }

            // One Shot - multiplier les dégâts de l'arme actuelle
            if (menuGUI.OneShot)
            {
                Player localPlayer = Player.m_localPlayer;
                if (localPlayer != null)
                {
                    // Modifier les dégâts de l'arme équipée
                    ItemDrop.ItemData currentWeapon = localPlayer.GetCurrentWeapon();
                    if (currentWeapon != null && currentWeapon.m_shared != null)
                    {
                        // Multiplier tous les types de dégâts par 9999
                        currentWeapon.m_shared.m_damages.m_damage = 9999f;
                        currentWeapon.m_shared.m_damages.m_blunt = 9999f;
                        currentWeapon.m_shared.m_damages.m_slash = 9999f;
                        currentWeapon.m_shared.m_damages.m_pierce = 9999f;
                        currentWeapon.m_shared.m_damages.m_chop = 9999f;
                        currentWeapon.m_shared.m_damages.m_pickaxe = 9999f;
                        currentWeapon.m_shared.m_damages.m_fire = 9999f;
                        currentWeapon.m_shared.m_damages.m_frost = 9999f;
                        currentWeapon.m_shared.m_damages.m_lightning = 9999f;
                        currentWeapon.m_shared.m_damages.m_poison = 9999f;
                        currentWeapon.m_shared.m_damages.m_spirit = 9999f;
                    }
                }
            }

            // Speed Hack - modifier les champs Character
            if (menuGUI.SpeedHack)
            {
                Player localPlayer = Player.m_localPlayer;
                if (localPlayer != null)
                {
                    float speedMult = menuGUI.SpeedMultiplier;
                    Type charType = typeof(Character);
                    FieldInfo[] allFields = charType.GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                    
                    // Stocker les valeurs originales la première fois
                    if (!speedsStored)
                    {
                        originalSpeeds.Clear();
                        foreach (FieldInfo field in allFields)
                        {
                            if (field.FieldType == typeof(float))
                            {
                                string name = field.Name.ToLower();
                                if (name.Contains("speed") || name.Contains("acceleration"))
                                {
                                    float val = (float)field.GetValue(localPlayer);
                                    if (val > 0.1f && val < 50f)
                                    {
                                        originalSpeeds[field.Name] = val;
                                    }
                                }
                            }
                        }
                        speedsStored = true;
                        lastSpeedMultiplier = speedMult;
                    }
                    
                    // Appliquer seulement si le multiplicateur a changé
                    if (speedMult != lastSpeedMultiplier)
                    {
                        foreach (var kvp in originalSpeeds)
                        {
                            FieldInfo field = charType.GetField(kvp.Key, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                            if (field != null)
                            {
                                field.SetValue(localPlayer, kvp.Value * speedMult);
                            }
                        }
                        lastSpeedMultiplier = speedMult;
                    }
                }
            }
            else if (speedsStored)
            {
                // Réinitialiser les vitesses d'origine
                Player localPlayer = Player.m_localPlayer;
                if (localPlayer != null)
                {
                    Type charType = typeof(Character);
                    foreach (var kvp in originalSpeeds)
                    {
                        FieldInfo field = charType.GetField(kvp.Key, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                        if (field != null)
                        {
                            field.SetValue(localPlayer, kvp.Value);
                        }
                    }
                }
                speedsStored = false;
                lastSpeedMultiplier = 1f;
            }

            // Fly Hack - mode vol libre
            if (menuGUI.FlyHack)
            {
                Player localPlayer = Player.m_localPlayer;
                if (localPlayer != null)
                {
                    // Désactiver la gravité
                    Rigidbody body = GetPrivateField<Rigidbody>(localPlayer, "m_body");
                    if (body != null)
                    {
                        body.useGravity = false;
                        body.velocity = Vector3.zero; // Arrêter la chute
                    }

                    // Permettre le vol libre avec les touches
                    Vector3 moveDirection = Vector3.zero;
                    float flySpeed = 10f;

                    // Espace pour monter
                    if (Input.GetKey(KeyCode.Space))
                    {
                        moveDirection += Vector3.up;
                    }

                    // Ctrl pour descendre
                    if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
                    {
                        moveDirection += Vector3.down;
                    }

                    // Déplacement avant/arrière/gauche/droite (direction de la caméra)
                    if (mainCamera != null)
                    {
                        Vector3 forward = mainCamera.transform.forward;
                        Vector3 right = mainCamera.transform.right;
                        forward.y = 0;
                        right.y = 0;
                        forward.Normalize();
                        right.Normalize();

                        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.Z)) // Z pour AZERTY
                        {
                            moveDirection += forward;
                        }
                        if (Input.GetKey(KeyCode.S))
                        {
                            moveDirection -= forward;
                        }
                        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.Q)) // Q pour AZERTY
                        {
                            moveDirection -= right;
                        }
                        if (Input.GetKey(KeyCode.D))
                        {
                            moveDirection += right;
                        }
                    }

                    // Appliquer le mouvement
                    if (moveDirection != Vector3.zero)
                    {
                        localPlayer.transform.position += moveDirection.normalized * flySpeed * Time.deltaTime;
                    }
                }
            }
            else
            {
                // Réactiver la gravité quand désactivé
                Player localPlayer = Player.m_localPlayer;
                if (localPlayer != null)
                {
                    Rigidbody body = GetPrivateField<Rigidbody>(localPlayer, "m_body");
                    if (body != null && !body.useGravity)
                    {
                        body.useGravity = true;
                    }
                }
            }

            // Poids infini - approche agressive
            if (menuGUI.NoWeightLimit)
            {
                Player localPlayer = Player.m_localPlayer;
                if (localPlayer != null)
                {
                    Inventory inventory = localPlayer.GetInventory();
                    if (inventory != null)
                    {
                        float currentWeight = inventory.GetTotalWeight();
                        float targetMaxWeight = currentWeight + 10000f; // Toujours 10000 au-dessus
                        
                        // Essayer tous les champs possibles liés au poids
                        Type playerType = typeof(Player);
                        FieldInfo[] fields = playerType.GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                        
                        foreach (FieldInfo field in fields)
                        {
                            string fieldName = field.Name.ToLower();
                            if (fieldName.Contains("carry") || fieldName.Contains("weight"))
                            {
                                if (field.FieldType == typeof(float))
                                {
                                    field.SetValue(localPlayer, targetMaxWeight);
                                }
                            }
                        }
                    }
                }
            }


            // Scanner les entités seulement toutes les 0.5 secondes (optimisation)
            if (Time.time - lastScanTime > SCAN_INTERVAL)
            {
                lastScanTime = Time.time;
                ScanEntities();
            }
        }
 

        private void ScanEntities()
        {
            // Nettoyer les listes
            aiList.Clear();
            animalList.Clear();
            itemList.Clear();
            bossStoneList.Clear();
            playersList.Clear();

            // Récupérer les joueurs (méthode optimisée)
            List<Player> allPlayers = Player.GetAllPlayers();
            if (allPlayers != null)
            {
                playersList.AddRange(allPlayers);
            }

            // Scanner seulement si les options ESP correspondantes sont activées
            if (menuGUI.ShowAnimals)
            {
                AnimalAI[] animAI = FindObjectsOfType(typeof(AnimalAI)) as AnimalAI[];
                if (animAI != null)
                {
                    animalList.AddRange(animAI);
                }
            }

            if (menuGUI.ShowEnemies)
            {
                BaseAI[] allAI = FindObjectsOfType(typeof(BaseAI)) as BaseAI[];
                if (allAI != null)
                {
                    foreach (BaseAI ai in allAI)
                    {
                        if (!(ai is AnimalAI))
                        {
                            aiList.Add(ai);
                        }
                    }
                }
            }

            if (menuGUI.ShowItems)
            {
                // Limiter le nombre d'items scannés pour éviter les lags
                ItemDrop[] items = FindObjectsOfType(typeof(ItemDrop)) as ItemDrop[];
                if (items != null && items.Length > 0)
                {
                    Camera cam = Camera.main;
                    if (cam != null)
                    {
                        Vector3 camPos = cam.transform.position;
                        float maxItemDistance = menuGUI.MaxItemDistance;
                        
                        // Filtrer par distance d'abord (plus rapide), puis prendre les 50 premiers
                        var nearbyItems = items
                            .Where(item => item != null && Vector3.Distance(camPos, item.transform.position) <= maxItemDistance)
                            .Take(50); // Réduit à 50 pour moins de lag
                        
                        itemList.AddRange(nearbyItems);
                    }
                }
            }

            // BossStones: scanner moins fréquemment car moins critique
            // On les scanne seulement si le temps depuis le dernier scan > 5 secondes
            if (menuGUI.ShowBossStones && Time.time - lastScanTime < 0.1f)
            {
                GameObject[] allObjects = FindObjectsOfType<GameObject>();
                int count = 0;
                foreach (GameObject obj in allObjects)
                {
                    if (count++ > 500) break; // Limiter le nombre d'objets testés
                    
                    if (obj.name.Contains("BossStone") ||
                        obj.name.Contains("altar") ||
                        obj.name.Contains("Altar") ||
                        obj.name.Contains("Offering"))
                    {
                        bossStoneList.Add(obj);
                        if (bossStoneList.Count > 10) break; // Réduit à 10 pour moins de lag
                    }
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

        private void DuplicateSlot8Item()
        {
            Player localPlayer = Player.m_localPlayer;
            if (localPlayer == null)
            {
                if (MessageHud.instance != null)
                {
                    MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, "❌ Joueur introuvable!");
                }
                return;
            }

            Inventory inventory = localPlayer.GetInventory();
            if (inventory == null)
            {
                if (MessageHud.instance != null)
                {
                    MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, "❌ Inventaire introuvable!");
                }
                return;
            }

            // Le slot 8 correspond à l'index 7 (index commence à 0)
            ItemDrop.ItemData itemInSlot8 = inventory.GetItemAt(7, 0);
            
            if (itemInSlot8 == null)
            {
                if (MessageHud.instance != null)
                {
                    MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, "❌ Aucun item dans le slot 8!");
                }
                return;
            }

            // Multiplier la quantité
            int currentStack = itemInSlot8.m_stack;
            int newStack = currentStack * menuGUI.DuplicateMultiplier;
            
            // Vérifier la limite de stack
            int maxStack = itemInSlot8.m_shared.m_maxStackSize;
            if (newStack > maxStack)
            {
                newStack = maxStack;
            }

            itemInSlot8.m_stack = newStack;

            // Message de confirmation
            if (MessageHud.instance != null)
            {
                string itemName = itemInSlot8.m_shared.m_name;
                MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, $"✅ {itemName}: {currentStack} → {newStack}");
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

        // Méthode utilitaire pour récupérer des champs privés via réflexion
        private T GetPrivateField<T>(object obj, string fieldName) where T : class
        {
            FieldInfo field = obj.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            if (field != null)
            {
                return field.GetValue(obj) as T;
            }
            return null;
        }
    }
}
