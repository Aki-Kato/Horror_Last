using UnityEngine;
using System.Collections;

public class AudioAutoDisable : MonoBehaviour
{
    private AudioSource audioSource;
    private Coroutine routine;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }

    private void OnEnable()
    {
        if (audioSource == null) return;

        // Запускаем звук
        audioSource.Play();

        // Если звук уже в процессе — перегрузим корутину
        if (routine != null)
            StopCoroutine(routine);

        // Ждём окончания
        routine = StartCoroutine(WaitAndDisable());
    }

    private IEnumerator WaitAndDisable()
    {
        // Ждём пока звук играет
        while (audioSource.isPlaying)
            yield return null;

        // Отключаем объект
        gameObject.SetActive(false);
    }
}
