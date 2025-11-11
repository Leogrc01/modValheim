using UnityEngine;

namespace modValheim
{
    public class MenuGUI : MonoBehaviour
    {
        // État du menu
        private bool showMenu = false;
        private Rect menuRect = new Rect(20, 20, 300, 600);
        private int currentTab = 0; // 0 = ESP, 1 = Skills
        private string[] tabNames = { "ESP", "Skills" };

        // Options ESP
        public bool ShowAnimals { get; set; } = true;
        public bool ShowEnemies { get; set; } = true;
        public bool ShowPlayers { get; set; } = true;
        public bool ShowItems { get; set; } = true;
        public bool ShowBossStones { get; set; } = true;
        public bool ShowSnaplines { get; set; } = true;
        public bool ShowBoxes { get; set; } = true;
        public bool ShowDistances { get; set; } = true;

        // Distances maximales (en mètres)
        public float MaxEnemyDistance { get; set; } = 100f;
        public float MaxPlayerDistance { get; set; } = 100f;
        public float MaxAnimalDistance { get; set; } = 100f;
        public float MaxItemDistance { get; set; } = 50f;
        public float MaxBossStoneDistance { get; set; } = 300f;

        // Options Skills
        public bool UnlimitedStamina { get; set; } = false;
        public bool NoSkillDrain { get; set; } = false;
        public float SkillMultiplier { get; set; } = 1f;
        public bool ResetSkillsRequested { get; set; } = false;

        // Style
        private GUIStyle boxStyle;
        private GUIStyle labelStyle;
        private GUIStyle toggleStyle;
        private GUIStyle buttonStyle;
        private GUIStyle selectedButtonStyle;
        private bool stylesInitialized = false;

        private void InitializeStyles()
        {
            if (stylesInitialized) return;

            // Style pour la boîte du menu
            boxStyle = new GUIStyle(GUI.skin.box)
            {
                normal = { background = MakeTexture(2, 2, new Color(0.1f, 0.1f, 0.1f, 0.9f)) }
            };

            // Style pour les labels
            labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white },
                alignment = TextAnchor.MiddleLeft
            };

            // Style pour les toggles
            toggleStyle = new GUIStyle(GUI.skin.toggle)
            {
                fontSize = 12,
                normal = { textColor = Color.white },
                onNormal = { textColor = Color.green }
            };

            // Style pour les boutons d'onglets
            buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white, background = MakeTexture(2, 2, new Color(0.3f, 0.3f, 0.3f, 0.8f)) },
                hover = { textColor = Color.white, background = MakeTexture(2, 2, new Color(0.4f, 0.4f, 0.4f, 0.8f)) }
            };

            // Style pour l'onglet sélectionné
            selectedButtonStyle = new GUIStyle(buttonStyle)
            {
                normal = { textColor = Color.green, background = MakeTexture(2, 2, new Color(0.2f, 0.5f, 0.2f, 0.8f)) },
                hover = { textColor = Color.green, background = MakeTexture(2, 2, new Color(0.2f, 0.5f, 0.2f, 0.8f)) }
            };

            stylesInitialized = true;
        }

        private Texture2D MakeTexture(int width, int height, Color color)
        {
            Color[] pixels = new Color[width * height];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = color;
            }

            Texture2D texture = new Texture2D(width, height);
            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        private void Update()
        {
            // Touche Insert pour afficher/cacher le menu
            if (Input.GetKeyDown(KeyCode.Insert))
            {
                showMenu = !showMenu;
            }
        }

        private void OnGUI()
        {
            if (!showMenu) return;

            InitializeStyles();

            // Dessiner le menu
            menuRect = GUI.Window(0, menuRect, DrawMenu, "Valheim Mod Menu", boxStyle);
        }

        private void DrawMenu(int windowID)
        {
            GUILayout.Space(10);

            // Onglets
            GUILayout.BeginHorizontal();
            for (int i = 0; i < tabNames.Length; i++)
            {
                if (GUILayout.Button(tabNames[i], currentTab == i ? selectedButtonStyle : buttonStyle))
                {
                    currentTab = i;
                }
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(10);

            // Afficher le contenu selon l'onglet sélectionné
            switch (currentTab)
            {
                case 0:
                    DrawESPTab();
                    break;
                case 1:
                    DrawSkillsTab();
                    break;
            }

            // Informations (toujours affichées)
            GUILayout.Space(10);
            GUILayout.Label("Touches:", labelStyle);
            GUILayout.Label("  Insert - Ouvrir/Fermer le menu", GUI.skin.label);
            GUILayout.Label("  Delete - Décharger le mod", GUI.skin.label);

            // Rendre le menu déplaçable
            GUI.DragWindow(new Rect(0, 0, 10000, 20));
        }

        private void DrawESPTab()
        {
            GUILayout.Label("Options d'affichage", labelStyle);
            GUILayout.Space(10);

            // Options ESP
            ShowEnemies = GUILayout.Toggle(ShowEnemies, " Afficher les ennemis (AI)", toggleStyle);
            if (ShowEnemies)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label($"  Distance: {MaxEnemyDistance:F0}m", GUI.skin.label);
                GUILayout.EndHorizontal();
                MaxEnemyDistance = GUILayout.HorizontalSlider(MaxEnemyDistance, 10f, 500f);
            }
            GUILayout.Space(5);

            ShowPlayers = GUILayout.Toggle(ShowPlayers, " Afficher les joueurs", toggleStyle);
            if (ShowPlayers)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label($"  Distance: {MaxPlayerDistance:F0}m", GUI.skin.label);
                GUILayout.EndHorizontal();
                MaxPlayerDistance = GUILayout.HorizontalSlider(MaxPlayerDistance, 10f, 500f);
            }
            GUILayout.Space(5);

            ShowAnimals = GUILayout.Toggle(ShowAnimals, " Afficher les animaux", toggleStyle);
            if (ShowAnimals)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label($"  Distance: {MaxAnimalDistance:F0}m", GUI.skin.label);
                GUILayout.EndHorizontal();
                MaxAnimalDistance = GUILayout.HorizontalSlider(MaxAnimalDistance, 10f, 500f);
            }
            GUILayout.Space(5);

            ShowItems = GUILayout.Toggle(ShowItems, " Afficher les items", toggleStyle);
            if (ShowItems)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label($"  Distance: {MaxItemDistance:F0}m", GUI.skin.label);
                GUILayout.EndHorizontal();
                MaxItemDistance = GUILayout.HorizontalSlider(MaxItemDistance, 10f, 200f);
            }
            GUILayout.Space(5);

            ShowBossStones = GUILayout.Toggle(ShowBossStones, " Afficher les Boss Stones", toggleStyle);
            if (ShowBossStones)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label($"  Distance: {MaxBossStoneDistance:F0}m", GUI.skin.label);
                GUILayout.EndHorizontal();
                MaxBossStoneDistance = GUILayout.HorizontalSlider(MaxBossStoneDistance, 10f, 1000f);
            }
            GUILayout.Space(5);

            GUILayout.Space(10);
            GUILayout.Label("Options visuelles", labelStyle);
            GUILayout.Space(10);

            ShowBoxes = GUILayout.Toggle(ShowBoxes, " Afficher les boîtes", toggleStyle);
            GUILayout.Space(5);

            ShowSnaplines = GUILayout.Toggle(ShowSnaplines, " Afficher les snaplines", toggleStyle);
            GUILayout.Space(10);

            ShowDistances = GUILayout.Toggle(ShowDistances, " Afficher les distances", toggleStyle);
            GUILayout.Space(10);
        }

        private void DrawSkillsTab()
        {
            GUILayout.Label("Options de compétences", labelStyle);
            GUILayout.Space(10);

            UnlimitedStamina = GUILayout.Toggle(UnlimitedStamina, " Stamina infinie", toggleStyle);
            GUILayout.Space(5);

            NoSkillDrain = GUILayout.Toggle(NoSkillDrain, " Pas de perte de skill à la mort", toggleStyle);
            GUILayout.Space(5);

            GUILayout.Space(10);
            GUILayout.Label("Multiplicateur de progression", labelStyle);
            GUILayout.BeginHorizontal();
            GUILayout.Label($"  x{SkillMultiplier:F1}", GUI.skin.label);
            GUILayout.EndHorizontal();
            SkillMultiplier = GUILayout.HorizontalSlider(SkillMultiplier, 1f, 10f);
            GUILayout.Space(10);

            if (GUILayout.Button("Réinitialiser tous les skills", buttonStyle))
            {
                ResetSkillsRequested = true;
            }
            
            if (GUILayout.Button("Maximiser tous les skills (100)", buttonStyle))
            {
                MaximizeAllSkills();
            }
        }

        private void MaximizeAllSkills()
        {
            Player localPlayer = Player.m_localPlayer;
            if (localPlayer == null) return;

            Skills skills = localPlayer.GetSkills();
            if (skills == null) return;

            // Récupérer toutes les skills et les mettre au max
            foreach (Skills.Skill skill in skills.GetSkillList())
            {
                skill.m_level = 100f;
                skill.m_accumulator = 0f;
            }
        }
    }
}
