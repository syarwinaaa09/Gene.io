using UnityEngine;

/// <summary>
/// Handles UI game over logic. Currently only activates the game over canvas on the PlayerController.GameOverEvent.
/// </summary>
public class UIGameOver : MonoBehaviour
{
    private Canvas _gameOverCanvas;

    private void Start()
    {
        _gameOverCanvas = GetComponent<Canvas>();
    }

    private void OnEnable()
    {
        PlayerController.GameOverEvent += GameOver;
    }

    private void OnDisable()
    {
        PlayerController.GameOverEvent -= GameOver;
    }

    /// <summary>
    /// Enables the Game Over canvas.
    /// </summary>
    private void GameOver()
    {
        _gameOverCanvas.enabled = true;
    }
}
