using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace EntilandVR.DosCuatro.DAM_AJEI.G_Uno
{
    /// <summary>
    /// Manager central de la galería de tiro, se encarga de registrar los hits, gestionar niveles y el momento del juego
    /// </summary>
    public class ShootingGalleryGameManager : MonoBehaviour
    {
        public static ShootingGalleryGameManager Instance { get; private set; }

        [Header("Lives")]
        [SerializeField] private int maxLives = 3;
        [SerializeField] private int startingLives = 3;

        [Header("Level")]
        [SerializeField] private int startingLevel = 0;
        [SerializeField] private int maxLevel = 5;
        [SerializeField] private bool automaticLevelProgression = true;
        [SerializeField] private float levelDurationSeconds = 20f;

        [Header("UI - Dynamic Values")]
        [SerializeField] private Image[] heartImages;
        [SerializeField] private TMP_Text scoreAmountText;
        [SerializeField] private TMP_Text levelAmountText;
        [SerializeField] private string levelPrefix = "Nivel ";

        [Header("UI - End Screen")]
        [SerializeField] private GameObject endGameUiRoot;
        [SerializeField] private TMP_Text resultMessageText;
        [SerializeField] private string winMessage = "You Win";
        [SerializeField] private string loseMessage = "You Lose";

        [Header("Lanes")]
        [SerializeField] private LaneController[] laneControllers;

        private int currentLives = 0;
        private int currentScore = 0;
        private int currentLevel = 0;
        private float levelTimer = 0f;
        private bool isGameOver = false;
        private bool isWin = false;

        public int CurrentLives
        {
            get { return currentLives; }
        }

        public int CurrentScore
        {
            get { return currentScore; }
        }

        public int CurrentLevel
        {
            get { return currentLevel; }
        }

        public bool IsGameplayRunning
        {
            get { return !isGameOver && !isWin; }
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void Start()
        {
            InitializeGame();
        }

        private void Update()
        {
            if (!IsGameplayRunning)
            {
                return;
            }

            if (!automaticLevelProgression)
            {
                return;
            }

            levelTimer += Time.deltaTime;
            if (levelTimer >= levelDurationSeconds)
            {
                levelTimer = 0f;
                AdvanceLevel();
            }
        }

        public void InitializeGame()
        {
            currentLives = Mathf.Clamp(startingLives, 1, Mathf.Max(1, maxLives));
            currentScore = 0;
            currentLevel = Mathf.Max(0, startingLevel);
            levelTimer = 0f;
            isGameOver = false;
            isWin = false;

            HideEndUi();
            ApplyLevelToLanes();
            RefreshAllUI();
        }

        /// <summary>
        /// Registra el hit en una diana
        /// </summary>
        public void RegisterTargetHit(int scoreDelta, int lifeDelta)
        {
            if (!IsGameplayRunning)
            {
                return;
            }

            AddScore(scoreDelta);
            ApplyLifeDelta(lifeDelta);
        }

        /// <summary>
        /// Registra el hit del bandido
        /// </summary>
        public void RegisterBanditHit(int scoreDelta)
        {
            if (!IsGameplayRunning)
            {
                return;
            }

            AddScore(scoreDelta);
        }

        /// <summary>
        /// Quita vidas al jugador
        /// </summary>
        public void DamagePlayer(int amount)
        {
            if (!IsGameplayRunning)
            {
                return;
            }

            if (amount <= 0)
            {
                return;
            }

            ApplyLifeDelta(-amount);
        }

        /// <summary>
        /// Da vidas al jugador
        /// </summary>
        public void HealPlayer(int amount)
        {
            if (!IsGameplayRunning)
            {
                return;
            }

            if (amount <= 0)
            {
                return;
            }

            ApplyLifeDelta(amount);
        }

        /// <summary>
        /// Sube un nivel. Si alcanza el nivel maximo, se gana la partida
        /// </summary>
        public void AdvanceLevel()
        {
            if (!IsGameplayRunning)
            {
                return;
            }

            currentLevel++;
            RefreshLevelUI();

            if (currentLevel >= maxLevel)
            {
                TriggerWin();
                return;
            }

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX(AudioManager.SFX_Sounds.NEXT_LEVEL);
            }

            ApplyLevelToLanes();
        }

        /// <summary>
        /// Reinicia el nivel
        /// </summary>
        public void PlayAgain()
        {
            Scene currentScene = SceneManager.GetActiveScene();
            SceneManager.LoadScene(currentScene.buildIndex);
        }

        private void AddScore(int amount)
        {
            currentScore += amount;
            if (currentScore < 0)
            {
                currentScore = 0;
            }

            RefreshScoreUI();
        }

        private void ApplyLifeDelta(int delta)
        {
            currentLives += delta;
            currentLives = Mathf.Clamp(currentLives, 0, Mathf.Max(1, maxLives));

            RefreshLivesUI();

            if (currentLives <= 0)
            {
                TriggerGameOver();
            }
        }

        private void TriggerGameOver()
        {
            isGameOver = true;
            ShowResultMessage(loseMessage);

            if (AudioManager.Instance != null)
            {
                // AudioManager.Instance.PlaySFX(AudioManager.SFX_Sounds.LOSE);
            }
        }

        private void TriggerWin()
        {
            isWin = true;
            ShowResultMessage(winMessage);

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX(AudioManager.SFX_Sounds.WIN);
            }
        }

        private void ShowResultMessage(string message)
        {
            if (endGameUiRoot != null)
            {
                endGameUiRoot.SetActive(true);
            }

            if (resultMessageText != null)
            {
                resultMessageText.text = message;
                resultMessageText.gameObject.SetActive(true);
            }
        }

        private void HideEndUi()
        {
            if (endGameUiRoot != null)
            {
                endGameUiRoot.SetActive(false);
            }

            if (resultMessageText != null)
            {
                resultMessageText.text = string.Empty;
                resultMessageText.gameObject.SetActive(false);
            }
        }

        private void ApplyLevelToLanes()
        {
            if (laneControllers == null)
            {
                return;
            }

            for (int i = 0; i < laneControllers.Length; i++)
            {
                LaneController currentLane = laneControllers[i];
                if (currentLane != null)
                {
                    currentLane.ApplyLevel(currentLevel);
                }
            }
        }

        private void RefreshAllUI()
        {
            RefreshLivesUI();
            RefreshScoreUI();
            RefreshLevelUI();
        }

        private void RefreshLivesUI()
        {
            if (heartImages == null)
            {
                return;
            }

            for (int i = 0; i < heartImages.Length; i++)
            {
                if (heartImages[i] == null)
                {
                    continue;
                }

                heartImages[i].enabled = i < currentLives;
            }
        }

        private void RefreshScoreUI()
        {
            if (scoreAmountText != null)
            {
                scoreAmountText.text = currentScore.ToString();
            }
        }

        private void RefreshLevelUI()
        {
            if (levelAmountText != null)
            {
                levelAmountText.text = levelPrefix + currentLevel.ToString();
            }
        }
    }
}