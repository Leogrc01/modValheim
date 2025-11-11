using UnityEngine;

namespace modValheim
{
    public class MenuGUI : MonoBehaviour
    {
        // État du menu
        private bool showMenu = false;
        private Rect menuRect = new Rect(20, 20, 300, 600);

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

        // Style
        private GUIStyle boxStyle;
        private GUIStyle labelStyle;
        private GUIStyle toggleStyle;
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
            menuRect = GUI.Window(0, menuRect, DrawMenu, "ESP Menu", boxStyle);
        }

        private void DrawMenu(int windowID)
        {
            GUILayout.Space(10);

            // Titre
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

            // Informations
            GUILayout.Space(10);
            GUILayout.Label("Touches:", labelStyle);
            GUILayout.Label("  Insert - Ouvrir/Fermer le menu", GUI.skin.label);
            GUILayout.Label("  Delete - Décharger le mod", GUI.skin.label);

            // Rendre le menu déplaçable
            GUI.DragWindow(new Rect(0, 0, 10000, 20));
        }
    }
}
