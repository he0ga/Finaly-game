using UnityEngine;

public class LookAtPlayer2D : MonoBehaviour
{
    public Transform player; // Переменная для ссылки на трансформ игрока

    void Update()
    {
        // Поворачиваем объект в сторону игрока
        transform.LookAt(player.position);

        // Можно ограничить вращение, чтобы поворачивать только вокруг оси Y (горизонтально)
        Vector3 rotation = transform.rotation.eulerAngles;
        rotation.x = 0;
        rotation.z = 0;
        transform.rotation = Quaternion.Euler(rotation);
    }
}
