using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[DefaultExecutionOrder(-1000)]
public class HP_view : MonoBehaviour
{
    public Image hpimg;
    public Image hpeffectImg;
    public float maxHp;
    public float currentHp;
    public float buffHpTime = 0.35f;

    private Coroutine updateCoroutine;

    [SerializeField]
    private CharacterData characterData;

    private void Awake()
    {
        characterData =FindObjectOfType<CharacterData>();

        characterData.OnDamaged += DecreaseHealth;
        characterData.OnRecover += IncreaseHealth;
    }
    void Start()
    {
      
        currentHp = characterData.Health;
        UpdateHP();


       
    }

    public void SetHealth(float health) {

        currentHp = Mathf.Clamp(health,0f,maxHp);
    
        UpdateHP();

        characterData.Health = currentHp;

    }

    public void IncreaseHealth(float amount)
    {

        SetHealth(currentHp + amount);

    }


    public void DecreaseHealth(float amount)
    {
        SetHealth(currentHp - amount);


    }

    private void UpdateHP()
    {
        hpimg.fillAmount = currentHp / maxHp;

        if (updateCoroutine != null)
        {


            StopCoroutine(updateCoroutine);

        }

        updateCoroutine = StartCoroutine(UpdateHpEffect());

    }

    private IEnumerator UpdateHpEffect()
    {

        float effectLength = hpeffectImg.fillAmount - hpimg.fillAmount;
        float elapsedTime = 0f;


        while (elapsedTime < buffHpTime && effectLength != 0)
        {

            elapsedTime += Time.deltaTime;
            hpeffectImg.fillAmount = Mathf.Lerp(hpimg.fillAmount + effectLength, hpimg.fillAmount, elapsedTime / buffHpTime);
            yield return null;
        }
        hpeffectImg.fillAmount = hpimg.fillAmount;

    }
}
