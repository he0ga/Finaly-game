using UnityEngine;

/// <summary>
/// Single quiz question with an arbitrary number of answer options.
/// Set <see cref="correctAnswerIndex"/> to the 0-based index of the right answer.
/// </summary>
[System.Serializable]
public class QuizQuestion
{
    [TextArea(2, 5)]
    public string questionText;

    public string[] answers;

    [Tooltip("0-based index of the correct answer in the answers array.")]
    public int correctAnswerIndex;
}
