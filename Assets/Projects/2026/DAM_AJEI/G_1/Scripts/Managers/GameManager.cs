using TMPro;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI livesText;

    public static int totalPoints = 0;
    [SerializeField] private int totalLives;

    public void AddPoints(int points)
    {
        totalPoints += points;
        scoreText.text = "Points: " + totalPoints;
    }
    public void AddLives(int lives)
    {
        totalLives += lives;
        livesText.text = "Lives: " + totalLives;
    }
}
