using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
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

    [SerializeField] private CharacterData characterData;

    private void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
        BindCharacterData();
    }

    private void Start()
    {
        RefreshDisplay();
    }

    public void SetHealth(float health)
    {
        if (characterData == null)
        {
            return;
        }

        characterData.Health = health;
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        BindCharacterData();
    }

    private void BindCharacterData()
    {
        var nextCharacterData = PersistentPlayer.Instance != null
            ? PersistentPlayer.Instance.CharacterData
            : FindObjectOfType<CharacterData>();

        if (nextCharacterData == characterData)
        {
            RefreshDisplay();
            return;
        }

        UnbindCharacterData();
        characterData = nextCharacterData;

        if (characterData == null)
        {
            Debug.LogWarning("HP_view: CharacterData not found for current scene.");
            return;
        }

        characterData.OnHealthChanged += HandleHealthChanged;
        RefreshDisplay();
    }

    private void RefreshDisplay()
    {
        if (characterData == null)
        {
            return;
        }

        maxHp = characterData.MaxHealth;
        currentHp = characterData.Health;
        UpdateHP();
    }

    private void HandleHealthChanged(float healthValue, float maxHealthValue)
    {
        maxHp = maxHealthValue;
        currentHp = healthValue;
        UpdateHP();
    }

    private void UpdateHP()
    {
        if (hpimg == null || hpeffectImg == null || maxHp <= 0f)
        {
            return;
        }

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

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        UnbindCharacterData();
    }

    private void UnbindCharacterData()
    {
        if (characterData == null)
        {
            return;
        }

        characterData.OnHealthChanged -= HandleHealthChanged;
    }
}
