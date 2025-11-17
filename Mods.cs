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
        private List<Player> playersList = new List<Player>();
        private List<BaseAI> aiList = new List<BaseAI>();
        private List<AnimalAI> animalList = new List<AnimalAI>();
        private List<ItemDrop> itemList = new List<ItemDrop>();
        private List<GameObject> bossStoneList = new List<GameObject>();
        private List<Pickable> resourceList = new List<Pickable>();
        private List<Destructible> oreList = new List<Destructible>();
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
        private bool lastOneShotValue = false;
        private bool lastNoWeightValue = false;
        private HitData.DamageTypes originalDamages;
        private bool originalDamagesStored = false;
        private ItemDrop.ItemData lastModifiedWeapon = null;
        
        // Vision nocturne
        private Light nightVisionLight = null;
        private float originalAmbientIntensity = 0f;
        private bool ambientIntensityStored = false;
        
        // Régénération améliorée
        private float originalHealthRegen = 0f;
        private float originalStaminaRegen = 0f;
        private bool regenStored = false;
        
        // Brightness
        private float originalBrightness = 1f;
        private bool brightnessStored = false;
        private Light brightnessLight = null;

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
            
            if (menuGUI.ShowResources)
            {
                // Afficher les Pickables (baies, champignons, etc.)
                foreach (Pickable resource in resourceList)
                {
                    if (resource != null)
                    {
                        DrawResourceESP(resource, Color.yellow, menuGUI.ShowDistances, menuGUI.MaxResourceDistance);
                    }
                }
                
                // Afficher les minerais
                foreach (Destructible ore in oreList)
                {
                    if (ore != null)
                    {
                        DrawOreESP(ore, new Color(0.5f, 0.5f, 0.5f), menuGUI.ShowDistances, menuGUI.MaxResourceDistance);
                    }
                }
            }
        }

        // Fonction pour dessiner l'ESP des ressources (Pickable)
        public void DrawResourceESP(Pickable resource, Color color, bool showDistance, float maxDistance)
        {
            if (mainCamera == null) return;
            
            float distance = Vector3.Distance(mainCamera.transform.position, resource.transform.position);
            if (distance > maxDistance) return;

            Vector3 position = resource.transform.position;
            Vector3 screenPos = mainCamera.WorldToScreenPoint(position);

            if (screenPos.z > 0f)
            {
                Vector2 pos2D = new Vector2(screenPos.x, Screen.height - screenPos.y);
                
                // Obtenir le nom de la ressource
                string resourceName = resource.GetHoverName();
                if (string.IsNullOrEmpty(resourceName))
                {
                    resourceName = resource.name.Replace("(Clone)", "").Trim();
                }
                
                string distanceText = showDistance ? $" [{distance:F1}m]" : "";
                string displayText = resourceName + distanceText;
                
                DrawText(pos2D, displayText, color);
            }
        }

        // Fonction pour dessiner l'ESP des minerais (Destructible)
        public void DrawOreESP(Destructible ore, Color color, bool showDistance, float maxDistance)
        {
            if (mainCamera == null) return;
            
            float distance = Vector3.Distance(mainCamera.transform.position, ore.transform.position);
            if (distance > maxDistance) return;

            Vector3 position = ore.transform.position;
            Vector3 screenPos = mainCamera.WorldToScreenPoint(position);

            if (screenPos.z > 0f)
            {
                Vector2 pos2D = new Vector2(screenPos.x, Screen.height - screenPos.y);
                
                // Obtenir tous les noms possibles
                string goName = ore.gameObject.name.Replace("(Clone)", "").Trim();
                string lowerName = goName.ToLower();
                string fullName = lowerName;
                
                // Ajouter le nom du parent
                if (ore.transform.parent != null)
                {
                    fullName += " " + ore.transform.parent.name.ToLower();
                }
                
                // Vérifier le HoverText
                HoverText hoverText = ore.GetComponent<HoverText>();
                if (hoverText != null && !string.IsNullOrEmpty(hoverText.m_text))
                {
                    fullName += " " + hoverText.m_text.ToLower();
                }
                
                // Déterminer le type et la couleur
                string oreName;
                if (fullName.Contains("copper"))
                {
                    oreName = "CUIVRE";
                    color = new Color(1f, 0.5f, 0f); // Orange vif
                }
                else if (fullName.Contains("tin"))
                {
                    oreName = "ÉTAIN";
                    color = new Color(0.85f, 0.85f, 0.85f); // Gris clair visible
                }
                else if (fullName.Contains("iron") || fullName.Contains("scrap") || fullName.Contains("mudpile"))
                {
                    oreName = "FER";
                    color = Color.white; // Blanc
                }
                else if (fullName.Contains("silver"))
                {
                    oreName = "ARGENT";
                    color = Color.white; // Blanc pur
                }
                else if (fullName.Contains("obsidian"))
                {
                    oreName = "OBSIDIENNE";
                    color = Color.magenta; // Magenta
                }
                else if (fullName.Contains("rock") || fullName.Contains("stone"))
                {
                    oreName = "ROCHE";
                    color = new Color(0.6f, 0.6f, 0.6f); // Gris moyen
                }
                else
                {
                    // Afficher le nom brut pour les minerais inconnus
                    oreName = goName.Replace("$piece_", "").Replace("deposit_", "").Replace("_", " ");
                    color = Color.yellow; // Jaune pour ce qui n'est pas reconnu
                }
                
                string distanceText = showDistance ? $" [{distance:F1}m]" : "";
                string displayText = oreName + distanceText;
                
                DrawText(pos2D, displayText, color);
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

            // Réparer tout si demandé
            if (menuGUI.RepairAllRequested)
            {
                RepairAllItems();
                menuGUI.RepairAllRequested = false;
            }

            // Spawn item si demandé
            if (menuGUI.SpawnItemRequested)
            {
                SpawnItem(menuGUI.SelectedItem, menuGUI.SpawnQuantity);
                menuGUI.SpawnItemRequested = false;
            }

            // Révéler la carte si demandé
            if (menuGUI.RevealMapRequested)
            {
                RevealFullMap();
                menuGUI.RevealMapRequested = false;
            }

            // Quick Stack si demandé
            if (menuGUI.QuickStackRequested)
            {
                QuickStackToNearbyContainers();
                menuGUI.QuickStackRequested = false;
            }

            // Réparer les structures si demandé
            if (menuGUI.RepairStructuresRequested)
            {
                RepairAllStructures();
                menuGUI.RepairStructuresRequested = false;
            }

            // Vision nocturne (Legit Cheat)
            ApplyNightVision();

            // Régénération améliorée (Legit Cheat)
            ApplyEnhancedRegen();

            // Pas de restrictions sur les portails
            if (menuGUI.NoPortalRestrictions)
            {
                AllowAllItemsThroughPortals();
            }

            // Pas de dégâts de chute
            if (menuGUI.NoFallDamage)
            {
                Player localPlayer = Player.m_localPlayer;
                if (localPlayer != null)
                {
                    // Annuler les dégâts de chute en mettant le flag à false
                    Character character = localPlayer as Character;
                    if (character != null)
                    {
                        SetPrivateField(character, "m_tolerateWater", true);
                        // Réinitialiser le timer de chute
                        SetPrivateField(character, "m_fallDamage", 0f);
                    }
                }
            }

            // Portée d'interaction augmentée (Legit Cheat)
            if (menuGUI.ExtendedReach)
            {
                Player localPlayer = Player.m_localPlayer;
                if (localPlayer != null)
                {
                    // Augmenter la portée d'interaction
                    Type playerType = typeof(Player);
                    FieldInfo[] fields = playerType.GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                    
                    foreach (FieldInfo field in fields)
                    {
                        string fieldName = field.Name.ToLower();
                        // Augmenter les distances d'interaction
                        if ((fieldName.Contains("maxinteractdistance") || 
                             fieldName.Contains("maxplacementdistance") ||
                             fieldName.Contains("interactrange")) && 
                            field.FieldType == typeof(float))
                        {
                            float originalValue = (float)field.GetValue(localPlayer);
                            if (originalValue > 0 && originalValue < 100)
                            {
                                field.SetValue(localPlayer, originalValue * menuGUI.ReachMultiplier);
                            }
                        }
                    }
                }
            }

            // Zoom caméra personnalisé (Legit Cheat)
            if (menuGUI.CustomCameraZoom)
            {
                // Modifier les limites de zoom de la caméra
                GameCamera gameCamera = GameCamera.instance;
                if (gameCamera != null)
                {
                    Type cameraType = typeof(GameCamera);
                    
                    // Modifier m_maxDistance (zoom max - molette arrière)
                    FieldInfo maxDistField = cameraType.GetField("m_maxDistance", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                    if (maxDistField != null)
                    {
                        maxDistField.SetValue(gameCamera, menuGUI.MaxZoomDistance);
                    }
                    
                    // Modifier m_minDistance (zoom min - molette avant)
                    FieldInfo minDistField = cameraType.GetField("m_minDistance", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                    if (minDistField != null)
                    {
                        minDistField.SetValue(gameCamera, menuGUI.MinZoomDistance);
                    }
                }
            }

            // Luminosité personnalisée (Legit Cheat)
            if (menuGUI.CustomBrightness)
            {
                Player localPlayer = Player.m_localPlayer;
                if (localPlayer != null)
                {
                    // Sauvegarder l'intensité ambiante d'origine
                    if (!brightnessStored)
                    {
                        originalBrightness = RenderSettings.ambientIntensity;
                        brightnessStored = true;
                    }
                    
                    // Augmenter l'éclairage ambiant
                    RenderSettings.ambientIntensity = originalBrightness * menuGUI.BrightnessValue;
                    
                    // Créer une lumière directionnelle pour simuler plus de luminosité
                    if (brightnessLight == null && menuGUI.BrightnessValue > 1.0f)
                    {
                        GameObject lightObj = new GameObject("BrightnessLight");
                        brightnessLight = lightObj.AddComponent<Light>();
                        brightnessLight.type = LightType.Directional;
                        brightnessLight.intensity = (menuGUI.BrightnessValue - 1.0f) * 0.5f; // Intensité basée sur le slider
                        brightnessLight.color = new Color(1f, 1f, 0.95f); // Blanc légèrement chaud
                        brightnessLight.shadows = LightShadows.None; // Pas d'ombres pour les perfs
                        // Orienter la lumière vers le bas (comme le soleil)
                        brightnessLight.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
                    }
                    
                    // Ajuster l'intensité de la lumière selon le slider
                    if (brightnessLight != null)
                    {
                        if (menuGUI.BrightnessValue > 1.0f)
                        {
                            brightnessLight.intensity = (menuGUI.BrightnessValue - 1.0f) * 0.5f;
                        }
                        else
                        {
                            // Si on diminue la luminosité, détruire la lumière supplémentaire
                            Destroy(brightnessLight.gameObject);
                            brightnessLight = null;
                        }
                    }
                }
            }
            else
            {
                // Désactiver la luminosité personnalisée
                if (brightnessStored)
                {
                    RenderSettings.ambientIntensity = originalBrightness;
                    brightnessStored = false;
                }
                
                // Détruire la lumière
                if (brightnessLight != null)
                {
                    Destroy(brightnessLight.gameObject);
                    brightnessLight = null;
                }
            }

            // Pouvoirs de boss infinis
            if (menuGUI.InfiniteGuardianPower)
            {
                Player localPlayer = Player.m_localPlayer;
                if (localPlayer != null)
                {
                    // Réinitialiser le cooldown du pouvoir de boss via réflexion
                    Type playerType = typeof(Player);
                    FieldInfo[] fields = playerType.GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                    
                    foreach (FieldInfo field in fields)
                    {
                        string fieldName = field.Name.ToLower();
                        // Chercher tous les champs liés au cooldown du pouvoir
                        if (fieldName.Contains("guardianpower") && fieldName.Contains("cooldown"))
                        {
                            if (field.FieldType == typeof(float))
                            {
                                field.SetValue(localPlayer, 0f);
                            }
                        }
                    }
                    
                    // Obtenir tous les StatusEffects actifs et réinitialiser ceux des Guardian Powers
                    SEMan seMan = localPlayer.GetSEMan();
                    if (seMan != null)
                    {
                        List<StatusEffect> allEffects = seMan.GetStatusEffects();
                        if (allEffects != null)
                        {
                            foreach (StatusEffect se in allEffects)
                            {
                                if (se != null && se.name != null)
                                {
                                    string seName = se.name.ToLower();
                                    // Vérifier si c'est un pouvoir de boss
                                    if (seName.Contains("gp_") || seName.Contains("guardianpower"))
                                    {
                                        // Réinitialiser le timer du pouvoir
                                        SetPrivateField(se, "m_time", 0f);
                                        SetPrivateField(se, "m_ttl", 999999f); // Durée quasi infinie
                                    }
                                }
                            }
                        }
                    }
                }
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
                    ItemDrop.ItemData currentWeapon = localPlayer.GetCurrentWeapon();
                    if (currentWeapon != null && currentWeapon.m_shared != null)
                    {
                        // Sauvegarder les dégâts d'origine la première fois
                        if (!originalDamagesStored || lastModifiedWeapon != currentWeapon)
                        {
                            originalDamages = currentWeapon.m_shared.m_damages.Clone();
                            originalDamagesStored = true;
                            lastModifiedWeapon = currentWeapon;
                        }

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
                lastOneShotValue = true;
            }
            else if (lastOneShotValue)
            {
                // Restaurer les dégâts d'origine
                Player localPlayer = Player.m_localPlayer;
                if (localPlayer != null && originalDamagesStored && lastModifiedWeapon != null)
                {
                    ItemDrop.ItemData currentWeapon = localPlayer.GetCurrentWeapon();
                    if (currentWeapon == lastModifiedWeapon && currentWeapon.m_shared != null)
                    {
                        currentWeapon.m_shared.m_damages = originalDamages.Clone();
                        if (MessageHud.instance != null)
                        {
                            MessageHud.instance.ShowMessage(MessageHud.MessageType.TopLeft, "✅ One Shot désactivé - Dégâts restaurés!");
                        }
                    }
                    originalDamagesStored = false;
                    lastModifiedWeapon = null;
                }
                lastOneShotValue = false;
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

            // Infinite Build - construction sans matériaux
            if (menuGUI.InfiniteBuild)
            {
                Player localPlayer = Player.m_localPlayer;
                if (localPlayer != null)
                {
                    // Activer le mode NoPlacementCost (construction gratuite)
                    SetPrivateField(localPlayer, "m_noPlacementCost", true);
                }
            }
            else
            {
                // Désactiver quand l'option est désactivée
                Player localPlayer = Player.m_localPlayer;
                if (localPlayer != null)
                {
                    SetPrivateField(localPlayer, "m_noPlacementCost", false);
                }
            }

            // Free Crafting - craft/amélioration sans ressources
            if (menuGUI.FreeCrafting)
            {
                Player localPlayer = Player.m_localPlayer;
                if (localPlayer != null)
                {
                    // Activer le mode NoCostCheat (craft gratuit)
                    SetPrivateField(localPlayer, "m_noPlacementCost", true);
                    
                    // Essayer d'activer aussi le flag de craft gratuit si disponible
                    Type playerType = typeof(Player);
                    FieldInfo[] fields = playerType.GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                    
                    foreach (FieldInfo field in fields)
                    {
                        string fieldName = field.Name.ToLower();
                        if (fieldName.Contains("nocost") && field.FieldType == typeof(bool))
                        {
                            field.SetValue(localPlayer, true);
                        }
                    }
                }
            }
            else
            {
                // Désactiver le free crafting
                Player localPlayer = Player.m_localPlayer;
                if (localPlayer != null && !menuGUI.InfiniteBuild) // Ne pas désactiver si InfiniteBuild est actif
                {
                    Type playerType = typeof(Player);
                    FieldInfo[] fields = playerType.GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                    
                    foreach (FieldInfo field in fields)
                    {
                        string fieldName = field.Name.ToLower();
                        if (fieldName.Contains("nocost") && field.FieldType == typeof(bool))
                        {
                            field.SetValue(localPlayer, false);
                        }
                    }
                }
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
                lastNoWeightValue = true;
            }
            else if (lastNoWeightValue)
            {
                // Réinitialiser les valeurs de poids par défaut
                Player localPlayer = Player.m_localPlayer;
                if (localPlayer != null)
                {
                    Type playerType = typeof(Player);
                    FieldInfo[] fields = playerType.GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                    
                    foreach (FieldInfo field in fields)
                    {
                        string fieldName = field.Name.ToLower();
                        if (fieldName.Contains("maxcarryweight"))
                        {
                            if (field.FieldType == typeof(float))
                            {
                                field.SetValue(localPlayer, 300f); // Valeur par défaut de Valheim
                            }
                        }
                    }
                    
                    if (MessageHud.instance != null)
                    {
                        MessageHud.instance.ShowMessage(MessageHud.MessageType.TopLeft, "🔄 Poids limité réactivé!");
                    }
                }
                lastNoWeightValue = false;
            }


            // Scanner les entités seulement toutes les 0.5 secondes (optimisation)
            if (Time.time - lastScanTime > SCAN_INTERVAL)
            {
                lastScanTime = Time.time;
                ScanEntities();
            }
        }
 

        // Helper pour identifier si un objet est un minerai
        private bool IsMineralResource(string name)
        {
            string lowerName = name.ToLower();
            
            // Exclure les arbres et plantes
            if (lowerName.Contains("tree") || lowerName.Contains("sapling") || 
                lowerName.Contains("beech") || lowerName.Contains("fir") ||
                lowerName.Contains("pine") || lowerName.Contains("oak"))
            {
                return false;
            }
            
            // Inclure les minerais
            return lowerName.Contains("rock") || 
                   lowerName.Contains("copper") ||
                   lowerName.Contains("tin") ||
                   lowerName.Contains("iron") ||
                   lowerName.Contains("silver") ||
                   lowerName.Contains("obsidian") ||
                   lowerName.Contains("deposit") ||
                   lowerName.Contains("ore") ||
                   lowerName.Contains("mudpile") || // Fer des cryptes
                   lowerName.Contains("scrap"); // Fer scrap
        }

        private void ScanEntities()
        {
            // Nettoyer les listes
            aiList.Clear();
            animalList.Clear();
            itemList.Clear();
            bossStoneList.Clear();
            resourceList.Clear();
            oreList.Clear();
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

            // Scanner les ressources (Pickable: baies, champignons, etc.)
            if (menuGUI.ShowResources)
            {
                Pickable[] pickables = FindObjectsOfType(typeof(Pickable)) as Pickable[];
                if (pickables != null && pickables.Length > 0)
                {
                    Camera cam = Camera.main;
                    if (cam != null)
                    {
                        Vector3 camPos = cam.transform.position;
                        float maxDist = menuGUI.MaxResourceDistance;
                        
                        var nearbyResources = pickables
                            .Where(p => p != null && Vector3.Distance(camPos, p.transform.position) <= maxDist)
                            .Take(50); // Limiter à 50
                        
                        resourceList.AddRange(nearbyResources);
                    }
                }
                
                // Scanner les minerais via Destructible (roches, arbres, etc.)
                Destructible[] destructibles = FindObjectsOfType(typeof(Destructible)) as Destructible[];
                if (destructibles != null && destructibles.Length > 0)
                {
                    Camera cam = Camera.main;
                    if (cam != null)
                    {
                        Vector3 camPos = cam.transform.position;
                        float maxDist = menuGUI.MaxResourceDistance;
                        
                        // Filtrer seulement les minerais (pas les arbres)
                        var nearbyOres = destructibles
                            .Where(d => d != null && 
                                   Vector3.Distance(camPos, d.transform.position) <= maxDist &&
                                   IsMineralResource(d.gameObject.name))
                            .Take(30);
                        
                        oreList.AddRange(nearbyOres);
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

        private void SpawnItem(string itemName, int quantity)
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

            // Obtenir le prefab de l'item
            GameObject prefab = ObjectDB.instance.GetItemPrefab(itemName);
            if (prefab == null)
            {
                if (MessageHud.instance != null)
                {
                    MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, $"❌ Item '{itemName}' introuvable!");
                }
                return;
            }

            // Obtenir le composant ItemDrop
            ItemDrop itemDrop = prefab.GetComponent<ItemDrop>();
            if (itemDrop == null)
            {
                if (MessageHud.instance != null)
                {
                    MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, $"❌ '{itemName}' n'est pas un item valide!");
                }
                return;
            }

            // Créer et ajouter l'item à l'inventaire
            Inventory inventory = localPlayer.GetInventory();
            if (inventory != null)
            {
                // Créer une nouvelle instance de l'ItemData
                ItemDrop.ItemData newItem = itemDrop.m_itemData.Clone();
                newItem.m_stack = quantity;
                newItem.m_durability = newItem.GetMaxDurability();

                // Ajouter l'item à l'inventaire
                bool success = inventory.AddItem(newItem);

                if (success)
                {
                    if (MessageHud.instance != null)
                    {
                        string displayName = itemDrop.m_itemData.m_shared.m_name;
                        MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, $"✅ {quantity}x {displayName} créé!");
                    }
                }
                else
                {
                    if (MessageHud.instance != null)
                    {
                        MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, "❌ Inventaire plein!");
                    }
                }
            }
        }

        private void RepairAllItems()
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

            int repairedCount = 0;
            
            // Réparer tous les items dans l'inventaire
            foreach (ItemDrop.ItemData item in inventory.GetAllItems())
            {
                if (item != null && item.m_shared.m_maxDurability > 0)
                {
                    // Vérifier si l'item a besoin de réparation
                    if (item.m_durability < item.GetMaxDurability())
                    {
                        item.m_durability = item.GetMaxDurability();
                        repairedCount++;
                    }
                }
            }

            // Message de confirmation
            if (MessageHud.instance != null)
            {
                if (repairedCount > 0)
                {
                    MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, $"✅ {repairedCount} objet(s) réparé(s)!");
                }
                else
                {
                    MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, "ℹ️ Rien à réparer!");
                }
            }
        }

        private void RevealFullMap()
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

            // Obtenir la minimap
            Minimap minimap = Minimap.instance;
            if (minimap == null)
            {
                if (MessageHud.instance != null)
                {
                    MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, "❌ Minimap introuvable!");
                }
                return;
            }

            try
            {
                // Utiliser la réflexion pour accéder à m_explored
                FieldInfo exploredField = typeof(Minimap).GetField("m_explored", BindingFlags.NonPublic | BindingFlags.Instance);
                if (exploredField == null)
                {
                    if (MessageHud.instance != null)
                    {
                        MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, "❌ Champ m_explored introuvable!");
                    }
                    return;
                }

                bool[] explored = exploredField.GetValue(minimap) as bool[];
                if (explored == null)
                {
                    if (MessageHud.instance != null)
                    {
                        MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, "❌ Impossible de récupérer m_explored!");
                    }
                    return;
                }

                // Révéler toute la carte
                for (int i = 0; i < explored.Length; i++)
                {
                    explored[i] = true;
                }

                // Forcer la mise à jour de la texture
                MethodInfo updateMethod = typeof(Minimap).GetMethod("UpdateTextureGeneration", BindingFlags.NonPublic | BindingFlags.Instance);
                if (updateMethod != null)
                {
                    updateMethod.Invoke(minimap, null);
                }

                // Message de confirmation
                if (MessageHud.instance != null)
                {
                    MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, "✅ Carte entièrement révélée!");
                }
            }
            catch (System.Exception ex)
            {
                if (MessageHud.instance != null)
                {
                    MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, $"❌ Erreur: {ex.Message}");
                }
            }
        }

        private void AllowAllItemsThroughPortals()
        {
            // Modifier tous les items dans l'inventaire pour qu'ils soient téléportables
            Player localPlayer = Player.m_localPlayer;
            if (localPlayer == null) return;

            Inventory inventory = localPlayer.GetInventory();
            if (inventory == null) return;

            // Parcourir tous les items
            foreach (ItemDrop.ItemData item in inventory.GetAllItems())
            {
                if (item != null && item.m_shared != null)
                {
                    // Forcer l'item à être téléportable
                    item.m_shared.m_teleportable = true;
                }
            }

            // Modifier aussi tous les prefabs d'items dans ObjectDB pour les rendre téléportables de base
            if (ObjectDB.instance != null)
            {
                foreach (GameObject itemPrefab in ObjectDB.instance.m_items)
                {
                    if (itemPrefab != null)
                    {
                        ItemDrop itemDrop = itemPrefab.GetComponent<ItemDrop>();
                        if (itemDrop != null && itemDrop.m_itemData != null && itemDrop.m_itemData.m_shared != null)
                        {
                            // Rendre tous les items téléportables
                            itemDrop.m_itemData.m_shared.m_teleportable = true;
                        }
                    }
                }
            }
        }

        private void ApplyEnhancedRegen()
        {
            if (menuGUI.EnhancedRegen)
            {
                Player localPlayer = Player.m_localPlayer;
                if (localPlayer == null) return;

                // Accéder aux champs de régénération via réflexion
                Type playerType = typeof(Player);
                
                // Sauvegarder les valeurs originales
                if (!regenStored)
                {
                    // Récupérer les valeurs de base
                    FieldInfo healthRegenField = playerType.GetField("m_baseHP", BindingFlags.NonPublic | BindingFlags.Instance);
                    if (healthRegenField != null)
                    {
                        originalHealthRegen = (float)healthRegenField.GetValue(localPlayer);
                    }
                    regenStored = true;
                }

                // Améliorer la régénération de santé en modifiant le timer
                // Valheim régénère la santé tous les X secondes, on accélère ce process
                float currentHealth = localPlayer.GetHealth();
                float maxHealth = localPlayer.GetMaxHealth();
                
                if (currentHealth < maxHealth && currentHealth > 0)
                {
                    // Ajouter de la santé progressivement (subtil)
                    float regenAmount = (maxHealth * 0.01f * menuGUI.RegenMultiplier) * Time.deltaTime;
                    localPlayer.Heal(regenAmount, true); // true = afficher les effets visuels
                }

                // Améliorer la régénération de stamina
                float currentStamina = localPlayer.GetStamina();
                float maxStamina = localPlayer.GetMaxStamina();
                
                if (currentStamina < maxStamina)
                {
                    // Utiliser la réflexion pour accéder au taux de régénération
                    FieldInfo staminaRegenField = playerType.GetField("m_staminaRegen", BindingFlags.NonPublic | BindingFlags.Instance);
                    if (staminaRegenField != null)
                    {
                        // Obtenir la valeur actuelle et la multiplier
                        float baseRegen = (float)staminaRegenField.GetValue(localPlayer);
                        staminaRegenField.SetValue(localPlayer, baseRegen * menuGUI.RegenMultiplier);
                    }
                }
            }
            else if (regenStored)
            {
                // Réinitialiser les valeurs de régénération
                Player localPlayer = Player.m_localPlayer;
                if (localPlayer != null)
                {
                    Type playerType = typeof(Player);
                    FieldInfo staminaRegenField = playerType.GetField("m_staminaRegen", BindingFlags.NonPublic | BindingFlags.Instance);
                    if (staminaRegenField != null)
                    {
                        // Réinitialiser la stamina regen (valeur par défaut Valheim: 5f)
                        staminaRegenField.SetValue(localPlayer, 5f);
                    }
                }
                regenStored = false;
            }
        }

        private void ApplyNightVision()
        {
            if (menuGUI.NightVision)
            {
                Player localPlayer = Player.m_localPlayer;
                if (localPlayer == null) return;

                // Sauvegarder l'intensité ambiante originale
                if (!ambientIntensityStored)
                {
                    originalAmbientIntensity = RenderSettings.ambientIntensity;
                    ambientIntensityStored = true;
                }

                // Augmenter l'éclairage ambiant (subtil)
                RenderSettings.ambientIntensity = originalAmbientIntensity * menuGUI.NightVisionIntensity;

                // Ajouter une lumière subtile autour du joueur si elle n'existe pas
                if (nightVisionLight == null)
                {
                    GameObject lightObj = new GameObject("NightVisionLight");
                    nightVisionLight = lightObj.AddComponent<Light>();
                    nightVisionLight.type = LightType.Point;
                    nightVisionLight.range = 15f + (menuGUI.NightVisionIntensity * 5f); // Portée adaptive
                    nightVisionLight.intensity = 0.3f + (menuGUI.NightVisionIntensity * 0.2f); // Intensité subtile
                    nightVisionLight.color = new Color(0.7f, 0.8f, 1f); // Bleuâtre légèrement
                    nightVisionLight.shadows = LightShadows.None; // Pas d'ombres pour les perfs
                }

                // Suivre le joueur
                if (nightVisionLight != null)
                {
                    nightVisionLight.transform.position = localPlayer.transform.position + Vector3.up * 1.5f;
                    // Ajuster dynamiquement selon le slider
                    nightVisionLight.range = 15f + (menuGUI.NightVisionIntensity * 5f);
                    nightVisionLight.intensity = 0.3f + (menuGUI.NightVisionIntensity * 0.2f);
                }
            }
            else
            {
                // Désactiver la vision nocturne
                if (ambientIntensityStored)
                {
                    RenderSettings.ambientIntensity = originalAmbientIntensity;
                    ambientIntensityStored = false;
                }

                // Détruire la lumière
                if (nightVisionLight != null)
                {
                    Destroy(nightVisionLight.gameObject);
                    nightVisionLight = null;
                }
            }
        }

        private void RepairAllStructures()
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

            // Trouver toutes les pièces de construction (WearNTear) à portée
            WearNTear[] allPieces = FindObjectsOfType<WearNTear>();
            int repairedCount = 0;
            int totalPieces = 0;
            
            foreach (WearNTear piece in allPieces)
            {
                if (piece == null) continue;
                
                // Vérifier la distance
                float distance = Vector3.Distance(localPlayer.transform.position, piece.transform.position);
                if (distance > menuGUI.RepairStructuresRange) continue;
                
                totalPieces++;
                
                // Essayer d'utiliser la méthode Repair() si disponible
                try
                {
                    MethodInfo repairMethod = typeof(WearNTear).GetMethod("Repair", BindingFlags.Public | BindingFlags.Instance);
                    if (repairMethod != null)
                    {
                        bool repaired = (bool)repairMethod.Invoke(piece, null);
                        if (repaired)
                        {
                            repairedCount++;
                        }
                    }
                    else
                    {
                        // Méthode alternative: forcer la santé au max
                        Type wearType = typeof(WearNTear);
                        FieldInfo healthField = wearType.GetField("m_health", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                        
                        if (healthField != null)
                        {
                            // Obtenir m_healthPercentage pour vérifier si endommagé
                            MethodInfo getHealthMethod = wearType.GetMethod("GetHealthPercentage", BindingFlags.Public | BindingFlags.Instance);
                            if (getHealthMethod != null)
                            {
                                float healthPercent = (float)getHealthMethod.Invoke(piece, null);
                                
                                if (healthPercent < 1f) // Si endommagé
                                {
                                    healthField.SetValue(piece, piece.m_health);
                                    repairedCount++;
                                }
                            }
                        }
                    }
                }
                catch (System.Exception ex)
                {
                    // Ignorer les erreurs et continuer
                    continue;
                }
            }

            // Message de confirmation
            if (MessageHud.instance != null)
            {
                if (repairedCount > 0)
                {
                    MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, 
                        $"✅ {repairedCount}/{totalPieces} structure(s) réparée(s)!");
                }
                else if (totalPieces > 0)
                {
                    MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, 
                        $"ℹ️ {totalPieces} structure(s) trouvée(s) mais déjà en bon état!");
                }
                else
                {
                    MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, 
                        $"❌ Aucune structure dans un rayon de {menuGUI.RepairStructuresRange:F0}m!");
                }
            }
        }

        private void QuickStackToNearbyContainers()
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

            Inventory playerInventory = localPlayer.GetInventory();
            if (playerInventory == null)
            {
                if (MessageHud.instance != null)
                {
                    MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, "❌ Inventaire introuvable!");
                }
                return;
            }

            // Trouver tous les conteneurs à portée
            Container[] allContainers = FindObjectsOfType<Container>();
            List<Container> nearbyContainers = new List<Container>();
            
            foreach (Container container in allContainers)
            {
                if (container == null || container.GetInventory() == null) continue;
                
                float distance = Vector3.Distance(localPlayer.transform.position, container.transform.position);
                if (distance <= menuGUI.QuickStackRange)
                {
                    nearbyContainers.Add(container);
                }
            }

            if (nearbyContainers.Count == 0)
            {
                if (MessageHud.instance != null)
                {
                    MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, "❌ Aucun coffre à portée!");
                }
                return;
            }

            int totalItemsMoved = 0;

            // Pour chaque item dans l'inventaire du joueur
            List<ItemDrop.ItemData> playerItems = new List<ItemDrop.ItemData>(playerInventory.GetAllItems());
            
            foreach (ItemDrop.ItemData playerItem in playerItems)
            {
                if (playerItem == null) continue;

                // Chercher dans chaque coffre si cet item y existe déjà
                foreach (Container container in nearbyContainers)
                {
                    Inventory containerInv = container.GetInventory();
                    if (containerInv == null) continue;

                    // Vérifier si le coffre contient déjà cet item
                    bool containerHasItem = false;
                    foreach (ItemDrop.ItemData containerItem in containerInv.GetAllItems())
                    {
                        if (containerItem != null && 
                            containerItem.m_shared.m_name == playerItem.m_shared.m_name)
                        {
                            containerHasItem = true;
                            break;
                        }
                    }

                    // Si le coffre contient cet item, essayer de l'y transférer
                    if (containerHasItem)
                    {
                        // Cloner l'item pour le transférer
                        ItemDrop.ItemData itemToMove = playerItem.Clone();
                        itemToMove.m_stack = playerItem.m_stack;

                        // Essayer d'ajouter au coffre
                        bool added = containerInv.AddItem(itemToMove);
                        
                        if (added)
                        {
                            // Retirer de l'inventaire du joueur
                            playerInventory.RemoveItem(playerItem);
                            totalItemsMoved += itemToMove.m_stack;
                            break; // Passer à l'item suivant
                        }
                    }
                }
            }

            // Message de confirmation
            if (MessageHud.instance != null)
            {
                if (totalItemsMoved > 0)
                {
                    MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, 
                        $"✅ {totalItemsMoved} item(s) rangé(s) dans {nearbyContainers.Count} coffre(s)!");
                }
                else
                {
                    MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, 
                        "ℹ️ Aucun item à ranger (coffres ne contiennent pas ces items)");
                }
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

        // Nettoyage quand le mod est déchargé
        private void OnDestroy()
        {
            CleanupModEffects();
        }

        private void CleanupModEffects()
        {
            Player localPlayer = Player.m_localPlayer;
            if (localPlayer != null)
            {
                // Réinitialiser la gravité
                Rigidbody body = GetPrivateField<Rigidbody>(localPlayer, "m_body");
                if (body != null)
                {
                    body.useGravity = true;
                }

                // Réinitialiser le God Mode
                if (localPlayer.InGodMode())
                {
                    localPlayer.SetGodMode(false);
                }

                // Réinitialiser le mode construction infinie et craft gratuit
                SetPrivateField(localPlayer, "m_noPlacementCost", false);
                
                // Récupérer les champs du joueur
                Type playerType = typeof(Player);
                FieldInfo[] fields = playerType.GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                
                // Réinitialiser tous les flags nocost
                foreach (FieldInfo field in fields)
                {
                    string fieldName = field.Name.ToLower();
                    if (fieldName.Contains("nocost") && field.FieldType == typeof(bool))
                    {
                        field.SetValue(localPlayer, false);
                    }
                }

                // Réinitialiser le poids
                foreach (FieldInfo field in fields)
                {
                    string fieldName = field.Name.ToLower();
                    if (fieldName.Contains("maxcarryweight"))
                    {
                        if (field.FieldType == typeof(float))
                        {
                            field.SetValue(localPlayer, 300f);
                        }
                    }
                }

                // Réinitialiser les vitesses
                if (originalSpeeds.Count > 0)
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

                // Réinitialiser les dégâts de l'arme
                if (originalDamagesStored && lastModifiedWeapon != null)
                {
                    ItemDrop.ItemData currentWeapon = localPlayer.GetCurrentWeapon();
                    if (currentWeapon == lastModifiedWeapon && currentWeapon.m_shared != null)
                    {
                        currentWeapon.m_shared.m_damages = originalDamages.Clone();
                    }
                }

                // Message de confirmation
                if (MessageHud.instance != null)
                {
                    MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, "✅ Mod déchargé - Effets réinitialisés!");
                }
            }

            // Réinitialiser la vision nocturne
            if (ambientIntensityStored)
            {
                RenderSettings.ambientIntensity = originalAmbientIntensity;
                ambientIntensityStored = false;
            }
            if (nightVisionLight != null)
            {
                Destroy(nightVisionLight.gameObject);
                nightVisionLight = null;
            }

            // Réinitialiser la régénération améliorée
            if (regenStored && localPlayer != null)
            {
                Type playerType = typeof(Player);
                FieldInfo staminaRegenField = playerType.GetField("m_staminaRegen", BindingFlags.NonPublic | BindingFlags.Instance);
                if (staminaRegenField != null)
                {
                    staminaRegenField.SetValue(localPlayer, 5f);
                }
                regenStored = false;
            }

            // Réinitialiser la luminosité
            if (brightnessStored)
            {
                RenderSettings.ambientIntensity = originalBrightness;
                brightnessStored = false;
            }
            
            // Détruire la lumière de brightness
            if (brightnessLight != null)
            {
                Destroy(brightnessLight.gameObject);
                brightnessLight = null;
            }

            // Note: Les dégâts des armes restent modifiés jusqu'à ce que vous les rééquipiez
            // C'est une limitation car les ItemData sont persistés
        }
    }
}
