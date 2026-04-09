using UnityEngine;

/// <summary>
/// ScriptableObject asset that holds the full list of quiz questions.
/// Create via Assets → Create → Game → Quiz Data.
/// Add as many questions as needed; each question supports any number of answers.
/// </summary>
[CreateAssetMenu(fileName = "QuizData", menuName = "Game/Quiz Data")]
public class QuizData : ScriptableObject
{
    [Tooltip("Questions displayed in order. Add or remove freely.")]
    public QuizQuestion[] questions;
}
