using System.Collections;
using UnityEngine;

public class FruitNinjaBomb : MonoBehaviour
{
    [SerializeField] private GameObject bombSprite;

    private void Start()
    {
        if (bombSprite != null)
            StartCoroutine(Anim());
    }

    private IEnumerator Anim()
    {
        while (true)
        {
            bombSprite.SetActive(true);
            yield return new WaitForSeconds(0.2f);
            bombSprite.SetActive(false);
            yield return new WaitForSeconds(0.2f);
        }
    }
}
